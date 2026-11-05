using System.Text.Json;

using Aris.Application.Accounts.Dtos;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.Application.Accounts;

/// <summary>
/// Account administration and self-service password change (<c>ACC-01</c>, <c>AUTH-01</c>).
/// </summary>
/// <remarks>
/// Each mutation runs inside <see cref="IUnitOfWork"/> together with its success audit
/// row (<c>CON-ARCH-07</c>). Expected failures return <c>Result</c>; passwords and hashes
/// never enter the audit trail (<c>CON-SEC-01</c>).
/// </remarks>
public class AccountService : IAccountService {
    private const string CreateAction = "CREATE_ACCOUNT";
    private const string UpdateAction = "UPDATE_ACCOUNT";
    private const string LockAction = "LOCK_ACCOUNT";
    private const string UnlockAction = "UNLOCK_ACCOUNT";
    private const string ResetPasswordAction = "RESET_PASSWORD";
    private const string ChangeOwnPasswordAction = "CHANGE_OWN_PASSWORD";

    private const int MinimumPasswordLength = 8;
    private const int MaxUsernameLength = 50;
    private const int MaxFullNameLength = 100;

    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IUserAccountRepository userAccountRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork) {
        _userAccountRepository = userAccountRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<AccountDto>>> GetAllAccountsAsync(
        int currentUserId,
        CancellationToken ct = default) {
        // Account listings are restricted to active administrators; checking
        // the actor before querying avoids exposing data to unauthorized users.
        if (!await IsActiveAdminAsync(currentUserId)) {
            return Result<List<AccountDto>>.Failure(
                "You do not have permission to view accounts.");
        }

        var accounts = await _userAccountRepository.GetAllAsync();
        var dtos = accounts.Select(ToDto).ToList();

        return Result<List<AccountDto>>.Success(dtos);
    }

    public async Task<Result<int>> CreateAccountAsync(
        string username,
        string fullName,
        string password,
        string role,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveAdminAsync(currentUserId)) {
            return Result<int>.Failure(
                "You do not have permission to create accounts.");
        }

        username = (username ?? string.Empty).Trim();
        fullName = (fullName ?? string.Empty).Trim();
        role = (role ?? string.Empty).Trim().ToUpperInvariant();

        // Normalize user-entered values before validating or checking uniqueness
        // so equivalent inputs are stored and compared consistently.
        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(password)) {
            return Result<int>.Failure("All fields are required.");
        }

        if (username.Length > MaxUsernameLength) {
            return Result<int>.Failure(
                $"Username must be {MaxUsernameLength} characters or fewer.");
        }

        if (fullName.Length > MaxFullNameLength) {
            return Result<int>.Failure(
                $"Full name must be {MaxFullNameLength} characters or fewer.");
        }

        if (password.Length < MinimumPasswordLength) {
            return Result<int>.Failure(
                $"Password must contain at least {MinimumPasswordLength} characters.");
        }

        if (role is not ("ADMIN" or "OFFICER")) {
            return Result<int>.Failure("Invalid account role.");
        }

        var existing = await _userAccountRepository.GetByUsernameAsync(username);
        if (existing is not null) {
            return Result<int>.Failure(
                $"Username '{username}' is already registered.");
        }

        var account = new UserAccount {
            Username = username,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Status = "ACTIVE",
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        // Use one correlation ID for the account mutation and its audit record.
        var correlationId = Guid.NewGuid();

        try {
            await _unitOfWork.ExecuteAsync(async token => {
                // The audit entry is inside the same unit of work as the insert,
                // so a successful account creation always has a matching audit.
                await _userAccountRepository.AddAsync(account);

                await WriteSuccessAuditAsync(
                    currentUserId,
                    CreateAction,
                    account.UserId,
                    correlationId,
                    new {
                        account.UserId,
                        account.Username,
                        account.Role,
                        account.Status
                    },
                    token);
            }, ct);

            return Result<int>.Success(account.UserId);
        } catch (Exception ex) {
            await LogFailureAsync(
                currentUserId, CreateAction, null, correlationId, ex, ct);

            return Result<int>.Failure(
                $"The account could not be created: {ex.Message}");
        }
    }


    public async Task<Result> UpdateAccountAsync(
        int userId,
        string username,
        string role,
        int currentUserId,
        CancellationToken ct = default) {
        // 1. Check administrator permission
        if (!await IsActiveAdminAsync(currentUserId)) {
            return Result.Failure(
                "You do not have permission to update accounts.");
        }

        // 2. Normalize input
        username = (username ?? string.Empty).Trim();
        role = (role ?? string.Empty).Trim().ToUpperInvariant();

        // 3. Validate input
        if (string.IsNullOrWhiteSpace(username)) {
            return Result.Failure("All fields are required.");
        }

        if (username.Length > MaxUsernameLength) {
            return Result.Failure(
                $"Username must be {MaxUsernameLength} characters or fewer.");
        }


        if (role is not ("ADMIN" or "OFFICER")) {
            return Result.Failure("Invalid account role.");
        }

        // 4. Find account
        var account = await _userAccountRepository.GetByIdAsync(userId);

        if (account is null) {
            return Result.Failure("Account not found.");
        }

        // 5. Check duplicate username
        var existing = await _userAccountRepository
            .GetByUsernameAsync(username);

        if (existing is not null && existing.UserId != userId) {
            return Result.Failure("Username is already registered.");
        }

        // Prevent administrator from removing their own admin role
        if (userId == currentUserId && role != "ADMIN") {
            return Result.Failure(
                "You cannot change your own administrator role.");
        }

        // 6. Save previous values for audit
        var oldUsername = account.Username;
        var oldRole = account.Role;

        var correlationId = Guid.NewGuid();

        // 7. Update account
        account.Username = username;
        account.Role = role;

        try {
            await _unitOfWork.ExecuteAsync(async token =>
            {
                await _userAccountRepository.UpdateAsync(account);

                await WriteSuccessAuditAsync(
                    currentUserId,
                    UpdateAction,
                    account.UserId,
                    correlationId,
                    new {
                        account.UserId,
                        oldUsername,
                        newUsername = username,
                        oldRole,
                        newRole = role
                    },
                    token);
            }, ct);

            return Result.Success();
        } catch (Exception ex) {
            await LogFailureAsync(
                currentUserId,
                UpdateAction,
                userId,
                correlationId,
                ex,
                ct);

            return Result.Failure(
                $"The account could not be updated: {ex.Message}");
        }
    }


    public async Task<Result> ChangeAccountStatusAsync(
        int userId,
        string status,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveAdminAsync(currentUserId)) {
            return Result.Failure(
                "You do not have permission to manage accounts.");
        }

        status = (status ?? string.Empty).Trim().ToUpperInvariant();

        if (status is not ("ACTIVE" or "LOCKED")) {
            return Result.Failure("Invalid account status.");
        }

        if (userId == currentUserId && status == "LOCKED") {
            // Prevent the last active administrator from locking out the
            // currently authenticated operator.
            return Result.Failure("You cannot disable your own account.");
        }

        var account = await _userAccountRepository.GetByIdAsync(userId);
        if (account is null) {
            return Result.Failure("Account not found.");
        }

        if (account.Status == status) {
            return Result.Failure($"The account already has status {status}.");
        }

        var previousStatus = account.Status;
        var action = status == "LOCKED" ? LockAction : UnlockAction;
        var correlationId = Guid.NewGuid();

        // Keep the previous value for the audit trail before applying the
        // requested state transition.
        account.Status = status;

        try {
            await _unitOfWork.ExecuteAsync(async token => {
                await _userAccountRepository.UpdateAsync(account);

                await WriteSuccessAuditAsync(
                    currentUserId,
                    action,
                    account.UserId,
                    correlationId,
                    new {
                        account.UserId,
                        account.Username,
                        from = previousStatus,
                        to = status
                    },
                    token);
            }, ct);

            return Result.Success();
        } catch (Exception ex) {
            await LogFailureAsync(
                currentUserId, action, userId, correlationId, ex, ct);

            return Result.Failure(
                $"The account status could not be changed: {ex.Message}");
        }
    }

    public async Task<Result> ResetPasswordAsync(
        int userId,
        string newPassword,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveAdminAsync(currentUserId)) {
            return Result.Failure(
                "You do not have permission to reset passwords.");
        }

        if (string.IsNullOrWhiteSpace(newPassword)
            || newPassword.Length < MinimumPasswordLength) {
            return Result.Failure(
                $"Password must contain at least {MinimumPasswordLength} characters.");
        }

        var account = await _userAccountRepository.GetByIdAsync(userId);
        if (account is null) {
            return Result.Failure("Account not found.");
        }

        var correlationId = Guid.NewGuid();

        // Administrative resets force the user through the self-service change
        // flow on the next sign-in and clear the previous change timestamp.
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.MustChangePassword = true;
        account.PasswordChangedAt = null;

        try {
            await _unitOfWork.ExecuteAsync(async token => {
                await _userAccountRepository.UpdateAsync(account);

                await WriteSuccessAuditAsync(
                    currentUserId,
                    ResetPasswordAction,
                    account.UserId,
                    correlationId,
                    new {
                        account.UserId,
                        account.Username,
                        mustChangePassword = true
                    },
                    token);
            }, ct);

            return Result.Success();
        } catch (Exception ex) {
            await LogFailureAsync(
                currentUserId, ResetPasswordAction, userId, correlationId, ex, ct);

            return Result.Failure(
                $"The password could not be reset: {ex.Message}");
        }
    }

    public async Task<Result> ChangeOwnPasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default) {
        var account = await _userAccountRepository.GetByIdAsync(userId);

        if (account is null || account.Status != "ACTIVE") {
            return Result.Failure("Account is not available.");
        }

        if (!account.MustChangePassword) {
            // This flow is intentionally limited to accounts that were marked
            // for a mandatory change, such as newly created or reset accounts.
            return Result.Failure("Password change is not required.");
        }

        if (string.IsNullOrWhiteSpace(currentPassword)) {
            return Result.Failure("Current password is required.");
        }

        bool isCurrentPasswordValid;

        try {
            // Treat malformed stored hashes as a verification failure rather
            // than allowing a hashing-library exception to escape the service.
            isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                currentPassword,
                account.PasswordHash);
        } catch (BCrypt.Net.SaltParseException) {
            return Result.Failure("Unable to verify the current password.");
        }

        if (!isCurrentPasswordValid) {
            return Result.Failure("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(newPassword)
            || newPassword.Length < MinimumPasswordLength) {
            return Result.Failure(
                $"Password must contain at least {MinimumPasswordLength} characters.");
        }

        if (BCrypt.Net.BCrypt.Verify(newPassword, account.PasswordHash)) {
            return Result.Failure(
                "New password must be different from the current password.");
        }

        var correlationId = Guid.NewGuid();

        // A successful self-service change clears the forced-change flag and
        // records when the new credential became active.
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.MustChangePassword = false;
        account.PasswordChangedAt = DateTime.UtcNow;

        try {
            await _unitOfWork.ExecuteAsync(async token => {
                await _userAccountRepository.UpdateAsync(account);

                await WriteSuccessAuditAsync(
                    userId,
                    ChangeOwnPasswordAction,
                    account.UserId,
                    correlationId,
                    new {
                        account.UserId,
                        account.Username
                    },
                    token);
            }, ct);

            return Result.Success();
        } catch (Exception ex) {
            await LogFailureAsync(
                userId, ChangeOwnPasswordAction, userId, correlationId, ex, ct);

            return Result.Failure(
                $"The password could not be changed: {ex.Message}");
        }
    }

    private async Task<bool> IsActiveAdminAsync(int userId) {
        var user = await _userAccountRepository.GetByIdAsync(userId);

        // Pattern matching keeps both authorization requirements together:
        // the actor must have the ADMIN role and an ACTIVE account status.
        return user is { Role: "ADMIN", Status: "ACTIVE" };
    }

    private async Task WriteSuccessAuditAsync(
        int actorUserId,
        string action,
        int entityId,
        Guid correlationId,
        object details,
        CancellationToken ct) {
        // Password values and hashes are deliberately absent from the supplied
        // details object; only operational metadata is serialized.
        await _auditService.LogAsync(new AuditEntry {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = "UserAccount",
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(details),
            CorrelationId = correlationId
        }, ct);
    }

    private async Task LogFailureAsync(
        int actorUserId,
        string action,
        int? entityId,
        Guid correlationId,
        Exception exception,
        CancellationToken ct) {
        // Failure audits are independent so an exception that rolls back the
        // business transaction is still recorded for operational follow-up.
        await _auditService.LogFailureIndependentAsync(new AuditFailureEntry {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = "UserAccount",
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(new {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, ct);
    }

    private static AccountDto ToDto(UserAccount account) => new() {
        UserId = account.UserId,
        Username = account.Username,
        FullName = account.FullName,
        Role = account.Role,
        Status = account.Status,
        CreatedAt = account.CreatedAt
    };
}