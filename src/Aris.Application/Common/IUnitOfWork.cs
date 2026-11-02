namespace Aris.Application.Common;

/// <summary>
/// Runs one application use case inside a single database transaction.
/// </summary>
/// <remarks>
/// Application services use this to keep a repository mutation and its success audit
/// atomic without referencing EF Core (<c>D18</c>, <c>CON-ARCH-07</c>). The implementation
/// lives in Infrastructure; the UI never sees it.
/// </remarks>
public interface IUnitOfWork {
    /// <summary>
    /// Executes <paramref name="action"/> inside one transaction and commits it. Any
    /// exception rolls the transaction back and is rethrown for the caller to map to a
    /// <c>Result</c>.
    /// </summary>
    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default);
}