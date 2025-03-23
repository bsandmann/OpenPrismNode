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

namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionUtxos
{
    public class GetApiTransactionUtxosHandler : IRequestHandler<GetApiTransactionUtxosRequest, Result<ApiResponseUtxo>>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetApiTransactionUtxosHandler> _logger;
        private readonly AppSettings _appSettings;
        private readonly IAppCache _cache;

        public GetApiTransactionUtxosHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<GetApiTransactionUtxosHandler> logger,
            IOptions<AppSettings> appSettings,
            IAppCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _cache = cache;
        }

        public async Task<Result<ApiResponseUtxo>> Handle(GetApiTransactionUtxosRequest request, CancellationToken cancellationToken)
        {
            // Build a cache key using the transaction hash for UTXOs
            string cacheKey = string.Concat(CacheKeys.Transaction_Utxos_By_Id, request.TxHash);

            // Use LazyCache to get or add the UTXO response
            return await _cache.GetOrAddAsync(
                cacheKey,
                async () => await FetchTransactionUtxosFromApi(request.TxHash, cancellationToken),
                TimeSpan.FromMinutes(2)); // Cache for 2 minutes
        }

        /// <summary>
        /// Fetch the UTXOs from the Blockfrost API (with retries/exception handling).
        /// </summary>
        private async Task<Result<ApiResponseUtxo>> FetchTransactionUtxosFromApi(string txHash, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Fetching transaction UTXOs with hash {TxHash} from Blockfrost API", txHash);

                // Create HttpClient
                var client = _httpClientFactory.CreateClient();

                // NOTE: This endpoint is typically: GET /txs/{hash}/utxos
                var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    _appSettings.Blockfrost.BaseUrl,
                    _appSettings.Blockfrost.ApiKey,
                    $"txs/{txHash}/utxos");

                // Send the request and deserialize the result
                var result = await BlockfrostHelper.SendBlockfrostRequestAsync<ApiResponseUtxo>(
                    client,
                    httpRequest,
                    _logger,
                    cancellationToken);

                if (result.IsFailed)
                {
                    // Bubble up any errors from the underlying call
                    return Result.Fail<ApiResponseUtxo>(result.Errors);
                }

                _logger.LogDebug("Successfully retrieved UTXOs for transaction hash {TxHash}", txHash);
                return Result.Ok(result.Value);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching UTXOs with hash {TxHash}", txHash);
                return Result.Fail<ApiResponseUtxo>($"HTTP error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error occurred while fetching UTXOs with hash {TxHash}", txHash);
                return Result.Fail<ApiResponseUtxo>($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching UTXOs with hash {TxHash}", txHash);
                return Result.Fail<ApiResponseUtxo>($"Unexpected error: {ex.Message}");
            }
        }
    }
}
