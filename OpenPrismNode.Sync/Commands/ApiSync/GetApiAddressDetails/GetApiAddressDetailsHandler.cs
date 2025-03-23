using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Implementations.Blockfrost;

namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiAddressDetails
{
    public class GetApiAddressDetailsHandler : IRequestHandler<GetApiAddressDetailsRequest, Result<ApiResponseAddress>>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetApiAddressDetailsHandler> _logger;
        private readonly AppSettings _appSettings;
        private readonly IAppCache _cache;

        public GetApiAddressDetailsHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<GetApiAddressDetailsHandler> logger,
            IOptions<AppSettings> appSettings,
            IAppCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _cache = cache;
        }

        public async Task<Result<ApiResponseAddress>> Handle(GetApiAddressDetailsRequest request, CancellationToken cancellationToken)
        {
            // Build a cache key using the address
            string cacheKey = string.Concat(CacheKeys.Address_Details_By_Id, request.Address);

            // Use LazyCache to get or add the address details
            return await _cache.GetOrAddAsync(
                cacheKey,
                async () => await FetchAddressDetailsFromApi(request.Address, cancellationToken),
                TimeSpan.FromMinutes(10)); // Cache for 1 minutes
        }

        /// <summary>
        /// Fetch the address details from the Blockfrost API (with retries/exception handling).
        /// </summary>
        private async Task<Result<ApiResponseAddress>> FetchAddressDetailsFromApi(string address, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Fetching address details for {Address} from Blockfrost API", address);

                // Create HttpClient
                var client = _httpClientFactory.CreateClient();

                // Typically: GET /addresses/{address}
                var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    _appSettings.Blockfrost.BaseUrl,
                    _appSettings.Blockfrost.ApiKey,
                    $"addresses/{address}");

                // Send the request and deserialize the result
                var result = await BlockfrostHelper.SendBlockfrostRequestAsync<ApiResponseAddress>(
                    client,
                    httpRequest,
                    _logger,
                    cancellationToken);

                if (result.IsFailed)
                {
                    // If the underlying call fails, bubble up the errors
                    return Result.Fail<ApiResponseAddress>(result.Errors);
                }

                _logger.LogDebug("Successfully retrieved address details for {Address}", address);
                return Result.Ok(result.Value);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching address details for {Address}", address);
                return Result.Fail<ApiResponseAddress>($"HTTP error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error occurred while fetching address details for {Address}", address);
                return Result.Fail<ApiResponseAddress>($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching address details for {Address}", address);
                return Result.Fail<ApiResponseAddress>($"Unexpected error: {ex.Message}");
            }
        }
    }
}
