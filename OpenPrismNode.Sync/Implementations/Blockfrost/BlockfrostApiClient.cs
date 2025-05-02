namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync;
using ResiliencePolicies = Core.Common.ResiliencePolicies;

/// <summary>
/// Helper for making Blockfrost API requests using the recommended approach.
/// 
/// Blockfrost API guidelines:
/// - All API requests must include a "project_id" in HTTP header for authentication
/// - Data is returned in ascending order by default (oldest first, newest last)
/// - Default pagination returns 100 results at a time
/// - All time and timestamp fields are in seconds of UNIX time
/// - All amounts are returned in Lovelaces (1 ADA = 1,000,000 Lovelaces)
/// - Addresses, accounts and pool IDs are in Bech32 format
/// - All values are case sensitive
/// - All hex encoded values are lower case
/// </summary>
public static class BlockfrostHelper
{
    /// <summary>
    /// Creates a standard HttpRequestMessage for Blockfrost API
    /// </summary>
    /// <param name="baseUrl">The base URL of the Blockfrost API</param>
    /// <param name="apiKey">The API key for authentication</param>
    /// <param name="endpoint">The API endpoint to call (without the base URL)</param>
    /// <param name="page">Optional page number for pagination (1-based)</param>
    /// <param name="count">Optional number of results per page</param>
    /// <param name="orderDesc">Optional flag to order results in descending order</param>
    /// <returns>A configured HttpRequestMessage</returns>
    public static HttpRequestMessage CreateBlockfrostRequest(
        string baseUrl,
        string apiKey,
        string endpoint,
        int? page = null,
        int? count = null,
        bool orderDesc = false)
    {
        // Build the query string for pagination and ordering
        var queryParams = new List<string>();

        if (page.HasValue && page.Value > 1)
        {
            queryParams.Add($"page={page.Value}");
        }

        if (count.HasValue && count.Value > 0 && count.Value != 100) // 100 is default
        {
            queryParams.Add($"count={count.Value}");
        }

        if (orderDesc)
        {
            queryParams.Add("order=desc");
        }

        string queryString = queryParams.Count > 0
            ? "?" + string.Join("&", queryParams)
            : string.Empty;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{baseUrl}/{endpoint}{queryString}"),
            Headers =
            {
                { "project_id", apiKey }, // Using lowercase "project_id" as per documentation
            },
        };

        // Add Accept header for JSON
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    /// <summary>
    /// Sends a request to the Blockfrost API and deserializes the response
    /// Uses the shared resilience policy for automatic retries on failures
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="client">The HttpClient to use</param>
    /// <param name="request">The request message to send</param>
    /// <param name="logger">Optional logger for detailed logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the deserialized response or an error</returns>
    public static async Task<Result<T>> SendBlockfrostRequestAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger?.LogDebug("Sending request to Blockfrost API: {Url}", request.RequestUri);

            // Use the Core project's resilience policy
            var retryPolicy = ResiliencePolicies.GetStandardRetryPolicy<HttpResponseMessage>(
                logger ?? CreateFallbackLogger(),
                response => !response.IsSuccessStatusCode);

            // Send the request with resilience policy
            var response = await retryPolicy.ExecuteAsync(() => 
            {
                // Create a new request for each retry attempt to avoid "Cannot send the same request multiple times" error
                var requestCopy = CopyHttpRequestMessage(request);
                return client.SendAsync(requestCopy, cancellationToken);
            });

            // Check specific Blockfrost error cases
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                BlockfrostError blockfrostError = null;
                try
                {
                    blockfrostError = JsonSerializer.Deserialize<BlockfrostError>(
                        errorContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Could not parse as Blockfrost error format
                }

                string errorMessage = blockfrostError != null
                    ? $"{blockfrostError.Error}: {blockfrostError.Message}"
                    : errorContent;

                logger?.LogError("Blockfrost API request failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorMessage);

                // Handle specific Blockfrost error cases
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        return Result.Fail<T>($"Invalid request: {errorMessage}");

                    case HttpStatusCode.PaymentRequired:
                        return Result.Fail<T>("Daily request limit exceeded");

                    case HttpStatusCode.Forbidden:
                        return Result.Fail<T>("Authentication failed, invalid project_id");

                    case HttpStatusCode.NotFound:
                        return Result.Fail<T>("Resource not found");

                    case (HttpStatusCode)418: // I'm a teapot - used for auto-ban
                        return Result.Fail<T>("Request rate too high, auto-banned");

                    case (HttpStatusCode)425: // Too Early
                        return Result.Fail<T>("Mempool is full");

                    case HttpStatusCode.TooManyRequests:
                        return Result.Fail<T>("Rate limited, too many requests");

                    case HttpStatusCode.InternalServerError:
                        return Result.Fail<T>("Blockfrost server error");

                    default:
                        return Result.Fail<T>($"API call failed with status {response.StatusCode}: {errorMessage}");
                }
            }

            // Deserialize the response
            var result = await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
                cancellationToken);
                
            if (result == null)
            {
                logger?.LogError("Failed to deserialize response from Blockfrost API");
                return Result.Fail<T>("Could not deserialize response from Blockfrost API");
            }
            
            return Result.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger?.LogError(ex, "HTTP request error: {ErrorMessage}", ex.Message);
            return Result.Fail<T>($"HTTP request error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "JSON deserialization error: {ErrorMessage}", ex.Message);
            return Result.Fail<T>($"JSON deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error calling Blockfrost API: {ErrorMessage}", ex.Message);
            return Result.Fail<T>($"Unexpected error calling Blockfrost API: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates a copy of an HttpRequestMessage since HttpRequestMessage can only be sent once
    /// </summary>
    /// <param name="request">The original request</param>
    /// <returns>A copy of the request</returns>
    private static HttpRequestMessage CopyHttpRequestMessage(HttpRequestMessage request)
    {
        var newRequest = new HttpRequestMessage
        {
            Method = request.Method,
            RequestUri = request.RequestUri,
            Content = request.Content,
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy properties
        foreach (var property in request.Properties)
        {
            newRequest.Properties.Add(property);
        }

        return newRequest;
    }
    
    /// <summary>
    /// Creates a fallback logger when none is provided
    /// </summary>
    /// <returns>A null logger that doesn't log anything</returns>
    private static ILogger CreateFallbackLogger()
    {
        return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Represents the error format returned by Blockfrost API
    /// </summary>
    private class BlockfrostError
    {
        [JsonPropertyName("status_code")] public int StatusCode { get; set; }

        [JsonPropertyName("error")] public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }
}