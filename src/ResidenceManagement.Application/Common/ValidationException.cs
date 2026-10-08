namespace ResidenceManagement.Application.Common;

/// <summary>
/// Thrown when input validation fails before a use case executes.
/// Prefer returning <see cref="Result"/> for expected failures; use this for
/// guard clauses and validator integration.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IEnumerable<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors.ToArray();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new[] { message };
    }

    public IReadOnlyList<string> Errors { get; }
}
