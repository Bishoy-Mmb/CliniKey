using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CliniKey.Application")]

namespace CliniKey.SharedKernel.Primitives;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    static abstract IResult Failure(Error error);
}

/// <summary>
/// Railway-oriented Result type for explicit error handling.
/// Forces callers to handle both success and failure paths without exceptions.
/// </summary>
public class Result : IResult
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A success result cannot contain an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failure result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
    static IResult IResult.Failure(Error error) => Failure(error);

    internal static TResult CreateFailure<TResult>(Error error) where TResult : IResult
    {
        return (TResult)TResult.Failure(error);
    }
}

/// <summary>
/// Generic result that wraps a value on the success path.
/// </summary>
public class Result<TValue> : Result, IResult
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    public static new Result<TValue> Failure(Error error) => new(default, false, error);
    static IResult IResult.Failure(Error error) => Failure(error);

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);
}
