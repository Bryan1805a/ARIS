namespace Aris.Application.Common;

/// <summary>
/// Outcome of an application operation that carries no payload.
/// Expected failures are returned, not thrown.
/// </summary>
// Basic Result object used across services and UI to communicate success or business failure without throwing exceptions.
public class Result
{
    protected Result(bool isSuccess, IEnumerable<string>? errors)
    {
        // Sanity check: cannot be successful while still holding errors
        if (isSuccess && errors is not null && errors.Any())
        {
            throw new InvalidOperationException("A successful result cannot carry errors.");
        }

        // Sanity check: if it failed, there must be at least one explanation why
        if (!isSuccess && (errors is null || !errors.Any()))
        {
            throw new InvalidOperationException("A failed result must carry at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors?.ToArray() ?? Array.Empty<string>();
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<string> Errors { get; }

    // Creates a successful result
    public static Result Success() => new(true, null);

    // Creates a failed result with one or more error messages
    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
}

/// <summary>
/// Outcome of an application operation that returns a value on success.
/// </summary>
// Generic version of Result that returns a payload (e.g., entity ID, DTO) on success.
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, IEnumerable<string>? errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    // Holds the payload value if successful; otherwise default/null
    public T? Value { get; }

    // Returns a successful result containing data
    public static Result<T> Success(T value) => new(true, value, null);

    // Returns a failed result with error messages and no payload
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);

    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
}
