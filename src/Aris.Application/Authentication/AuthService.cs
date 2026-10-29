
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;

namespace Aris.Application.Authentication;

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

    public async Task<Result<LoginResult>> LoginAsync(
        string username,
        string password)
    {
        var account = await _userAccountRepository
            .GetByUsernameAsync(username);

        var maxFailedLoginsValue = await _systemConfigurationRepository
    .GetValueAsync("MAX_FAILED_LOGINS");

        var lockoutMinutesValue = await _systemConfigurationRepository
            .GetValueAsync("LOCKOUT_MINUTES");

        if (!int.TryParse(maxFailedLoginsValue, out int maxFailedLogins)
            || maxFailedLogins <= 0
            || !int.TryParse(lockoutMinutesValue, out int lockoutMinutes)
            || lockoutMinutes <= 0)
        {
            return Failure<LoginResult>(
                "Authentication configuration is invalid. Please contact the administrator.");
        }

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

        if (account.Status == "LOCKED")
        {
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

        if (account.Status != "ACTIVE")
        {
            return Failure<LoginResult>(
                "This account is not active.");
        }


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

        if (account.FailedLoginCount > 0)
        {
            account.FailedLoginCount = 0;
            account.LockedUntil = null;

            await _userAccountRepository.UpdateAsync(account);
        }

        await _auditService.LogAsync(
            new AuditEntry
            {
                ActorUserId = account.UserId,
                Action = "LOGIN",
                EntityType = "UserAccount",
                EntityId = account.UserId,
                DetailsJson = "{\"reason\":\"LOGIN_SUCCESS\"}"
            });

        return Success(new LoginResult
        {
            UserId = account.UserId,
            Username = account.Username,
            Role = account.Role,
            MustChangePassword = account.MustChangePassword
        });
    }
}
