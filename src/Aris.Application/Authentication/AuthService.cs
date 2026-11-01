using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;

namespace Aris.Application.Authentication;

// Handles authentication (login, lockout checks, password verification)
public class AuthService : BaseService, IAuthService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly IAuditService _auditService;

    public AuthService(
        IUserAccountRepository userAccountRepository,
        ISystemConfigurationRepository systemConfigurationRepository,
        IAuditService auditService)
    {
        _userAccountRepository = userAccountRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
        _auditService = auditService;
    }

    // Authenticates the user with username and password, handles failed attempts and lockouts
    public async Task<Result<LoginResult>> LoginAsync(
        string username,
        string password)
    {
        // 1. Look up the account by username
        var account = await _userAccountRepository
            .GetByUsernameAsync(username);

        // 2. Read lockout configuration from the database so we don't hardcode security limits
        var maxFailedLoginsValue = await _systemConfigurationRepository
            .GetValueAsync("MAX_FAILED_LOGINS");

        var lockoutMinutesValue = await _systemConfigurationRepository
            .GetValueAsync("LOCKOUT_MINUTES");

        // Make sure the config values are valid positive numbers
        if (!int.TryParse(maxFailedLoginsValue, out int maxFailedLogins)
            || maxFailedLogins <= 0
            || !int.TryParse(lockoutMinutesValue, out int lockoutMinutes)
            || lockoutMinutes <= 0)
        {
            return Failure<LoginResult>(
                "Authentication configuration is invalid. Please contact the administrator.");
        }

        // 3. User does not exist - record audit failure and return generic error message
        if (account == null)
        {
            await _auditService.LogLoginFailureAsync(
                new AuditFailureEntry
                {
                    AttemptedUsername = username,
                    Action = "LOGIN",
                    EntityType = "UserAccount",
                    DetailsJson = "{\"reason\":\"INVALID_CREDENTIALS\"}"
                });

            return Failure<LoginResult>(
                "Invalid username or password.");
        }

        // 4. Handle account lockout state
        if (account.Status == "LOCKED")
        {
            // If the lockout timer has expired, automatically unlock the account
            if (account.LockedUntil.HasValue &&
                account.LockedUntil.Value <= DateTime.UtcNow)
            {
                account.Status = "ACTIVE";
                account.FailedLoginCount = 0;
                account.LockedUntil = null;

                await _userAccountRepository.UpdateAsync(account);
            }
            else
            {
                // Account is still in lockout window
                await _auditService.LogLoginFailureAsync(
                    new AuditFailureEntry
                    {
                        ActorUserId = account.UserId,
                        AttemptedUsername = username,
                        Action = "LOGIN",
                        EntityType = "UserAccount",
                        EntityId = account.UserId,
                        DetailsJson = "{\"reason\":\"ACCOUNT_LOCKED\"}"
                    });
                return Failure<LoginResult>(
                    "This account is temporarily locked. Please try again later.");
            }
        }

        // 5. Account must be ACTIVE to sign in
        if (account.Status != "ACTIVE")
        {
            return Failure<LoginResult>(
                "This account is not active.");
        }

        // 6. Verify password using BCrypt against the stored salted hash
        bool isPasswordValid;

        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(
                password,
                account.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return Failure<LoginResult>(
                "Authentication failed. Please contact the administrator.");
        }

        // 7. Wrong password - bump failed count and lock account if threshold is reached
        if (!isPasswordValid)
        {
            account.FailedLoginCount++;

            await _auditService.LogLoginFailureAsync(
                new AuditFailureEntry
                {
                    ActorUserId = account.UserId,
                    AttemptedUsername = username,
                    Action = "LOGIN",
                    EntityType = "UserAccount",
                    EntityId = account.UserId,
                    DetailsJson = "{\"reason\":\"INVALID_CREDENTIALS\"}"
                });

            // Reached max allowed failed attempts -> lock the account
            if (account.FailedLoginCount >= maxFailedLogins)
            {
                account.Status = "LOCKED";
                account.LockedUntil = DateTime.UtcNow
                    .AddMinutes(lockoutMinutes);

                await _userAccountRepository.UpdateAsync(account);

                return Failure<LoginResult>(
                    "This account is temporarily locked. Please try again later.");
            }

            await _userAccountRepository.UpdateAsync(account);

            return Failure<LoginResult>(
                "Invalid username or password.");
        }

        // 8. Password is valid! Reset failed login count if needed
        if (account.FailedLoginCount > 0)
        {
            account.FailedLoginCount = 0;
            account.LockedUntil = null;

            await _userAccountRepository.UpdateAsync(account);
        }

        // 9. Write audit log for successful login
        await _auditService.LogAsync(
            new AuditEntry
            {
                ActorUserId = account.UserId,
                Action = "LOGIN",
                EntityType = "UserAccount",
                EntityId = account.UserId,
                DetailsJson = "{\"reason\":\"LOGIN_SUCCESS\"}"
            });

        // 10. Return user info for the session (also flags if they need to change password)
        return Success(new LoginResult
        {
            UserId = account.UserId,
            Username = account.Username,
            Role = account.Role,
            MustChangePassword = account.MustChangePassword
        });
    }
}
