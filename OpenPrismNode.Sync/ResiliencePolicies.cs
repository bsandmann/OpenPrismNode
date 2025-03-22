using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace OpenPrismNode.Sync;

public static class ResiliencePolicies
{
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        // Define the retry intervals
        var retryDelays = new[]
        {
            TimeSpan.FromSeconds(5),    // Retry after 5 seconds
            TimeSpan.FromSeconds(15),   // Retry after 15 seconds
            TimeSpan.FromMinutes(1),    // Retry after 1 minute
            TimeSpan.FromMinutes(15),   // Retry after 15 minutes
            TimeSpan.FromMinutes(60)    // Retry after 60 minutes
        };

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>() // Handles network failures
            .OrResult(response =>
                response.StatusCode == HttpStatusCode.RequestTimeout || // 408
                ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)) // 5xx
            .WaitAndRetryAsync(
                retryDelays.Length,
                retryAttempt => retryDelays[retryAttempt - 1],
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var message = outcome.Exception?.Message ?? $"HTTP {outcome.Result.StatusCode}";
                    logger.LogWarning(
                        "Retry {RetryAttempt} after {Delay}s due to: {Message}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        message);
                });
    }
}