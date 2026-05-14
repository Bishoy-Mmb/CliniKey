using CliniKey.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace CliniKey.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var errors = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .ToList();

        if (errors.Any())
        {
            var error = errors.First();
            var validationError = Error.Validation(
                error.PropertyName,
                error.ErrorMessage);

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericArgument = resultType.GetGenericArguments()[0];
                var genericFailureMethod = typeof(Result).GetMethod("Failure", new[] { typeof(Error) })!
                    .MakeGenericMethod(genericArgument);
                return (TResponse)genericFailureMethod.Invoke(null, new object[] { validationError })!;
            }
            
            return (TResponse)Result.Failure(validationError);
        }

        return await next();
    }
}
