namespace CRM.Application.Common.Behaviours;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        var timer = new Stopwatch();
        timer.Start();

        try
        {
            return await next();
        }
        finally
        {
            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            _logger.LogInformation("Handled {RequestName} in {ElapsedMs} ms", requestName, elapsedMs);

            if (elapsedMs > 500)
            {
                _logger.LogWarning("Slow request {RequestName} ({ElapsedMs} ms)", requestName, elapsedMs);
            }
        }
    }
}
