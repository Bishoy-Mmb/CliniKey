using System.Collections.Generic;
using System.Linq;
using CliniKey.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace CliniKey.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly List<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToList();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationFailures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .ToList();

        if (validationFailures.Any())
        {
            var errors = validationFailures.Select(f => (f.PropertyName, f.ErrorMessage));
            var validationError = Error.Validation(errors);

            return Result.CreateFailure<TResponse>(validationError);
        }

        return await next();
    }
}
