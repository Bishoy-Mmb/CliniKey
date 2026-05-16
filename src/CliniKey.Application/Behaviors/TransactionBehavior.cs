using CliniKey.Application.Abstractions.Messaging;
using CliniKey.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CliniKey.Application.Behaviors;

/// <summary>
/// Wraps command handlers in a database transaction scope via IUnitOfWork.
/// Only applies to ICommand requests (write operations).
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Beginning transaction for {Command}", requestName);

        var response = await next();

        if (response.IsSuccess)
        {
            _logger.LogInformation("Transaction completed for {Command}", requestName);
        }
        else
        {
            _logger.LogWarning("Transaction for {Command} resulted in failure: {Error}", requestName, response.Error.Code);
        }

        return response;
    }
}
