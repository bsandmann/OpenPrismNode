using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Threading.Tasks;

namespace OpenPrismNode.Core.Common
{
    /// <summary>
    /// Provides reusable resilience policies for the Core project.
    /// </summary>
    public static class ResiliencePolicies
    {
        /// <summary>
        /// Creates a standard retry policy that retries operations with increasing delay intervals.
        /// Retries 3 times with delays of 2, 5, and 12 seconds.
        /// </summary>
        /// <typeparam name="TResult">The type of result the policy will handle.</typeparam>
        /// <param name="logger">Logger to log retry attempts.</param>
        /// <param name="resultPredicate">Optional predicate to determine which results should trigger a retry.</param>
        /// <returns>A configured retry policy.</returns>
        public static AsyncRetryPolicy<TResult> GetStandardRetryPolicy<TResult>(
            ILogger logger,
            Func<TResult, bool> resultPredicate = null)
        {
            // Define retry intervals - 2 seconds, 5 seconds, 12 seconds
            var retryDelays = new[]
            {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(12)
            };

            // Start with a policy that handles exceptions
            var policyBuilder = Policy<TResult>
                .Handle<Exception>(ex => 
                {
                    // Log exception
                    logger.LogWarning(ex, "Exception caught by retry policy: {Message}", ex.Message);
                    return true; // Retry all exceptions by default
                });

            // Add result predicate if provided
            if (resultPredicate != null)
            {
                policyBuilder = policyBuilder.OrResult(resultPredicate);
            }

            // Configure the retry policy with the specified delays
            return policyBuilder.WaitAndRetryAsync(
                retryDelays.Length,
                retryAttempt => retryDelays[retryAttempt - 1],
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    string outcomeDescription = outcome.Exception != null
                        ? $"Exception: {outcome.Exception.Message}"
                        : $"Result: {outcome.Result}";

                    logger.LogWarning(
                        "Retry {RetryAttempt}/{RetryCount} after {Delay}s due to: {Outcome}",
                        retryAttempt,
                        retryDelays.Length,
                        timespan.TotalSeconds,
                        outcomeDescription);

                    // Log context if available
                    if (context != null && context.Count > 0)
                    {
                        logger.LogDebug("Retry context: {@Context}", context);
                    }
                });
        }
    }
}