namespace Aris.Application.Common;

/// <summary>
/// Outcome of an application operation that carries no payload.
/// Expected failures are returned, not thrown.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, IEnumerable<string>? errors)
    {
        if (isSuccess && errors is not null && errors.Any())
        {
            throw new InvalidOperationException("A successful result cannot carry errors.");
        }

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

    public static Result Success() => new(true, null);

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
}

/// <summary>
/// Outcome of an application operation that returns a value on success.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, IEnumerable<string>? errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);

    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
}
