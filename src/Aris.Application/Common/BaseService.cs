namespace Aris.Application.Common;

/// <summary>
/// Base class for application services. Provides shared result helpers so
/// expected failures are returned as <see cref="Result"/> instead of thrown.
/// </summary>
public abstract class BaseService
{
    protected static Result<T> Success<T>(T value) => Result<T>.Success(value);

    protected static Result<T> Failure<T>(params string[] errors) => Result<T>.Failure(errors);

    protected static Result<T> NotFound<T>(string entityName, object id)
        => Result<T>.Failure($"{entityName} '{id}' was not found.");
}
