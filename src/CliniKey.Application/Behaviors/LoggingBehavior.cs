using System.Diagnostics;
using CliniKey.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CliniKey.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Executing request {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Request {RequestName} completed successfully in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Request {RequestName} failed in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Request {RequestName} threw an unhandled exception after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
