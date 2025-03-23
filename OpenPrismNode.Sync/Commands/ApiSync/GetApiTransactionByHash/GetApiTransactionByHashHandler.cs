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
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Implementations.Blockfrost;

namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionByHash
{
    public class GetApiTransactionByHashHandler : IRequestHandler<GetApiTransactionByHashRequest, Result<Transaction>>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetApiTransactionByHashHandler> _logger;
        private readonly AppSettings _appSettings;
        private readonly IAppCache _cache;

        public GetApiTransactionByHashHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<GetApiTransactionByHashHandler> logger,
            IOptions<AppSettings> appSettings,
            IAppCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _cache = cache;
        }

        public async Task<Result<Transaction>> Handle(GetApiTransactionByHashRequest request, CancellationToken cancellationToken)
        {
            // Build a cache key using the transaction hash
            string cacheKey = string.Concat(CacheKeys.Transaction_by_Id, request.TxHash);

            // Use LazyCache to get or add the transaction
            return await _cache.GetOrAddAsync(
                cacheKey,
                async () => await FetchTransactionFromApi(request.TxHash, cancellationToken),
                TimeSpan.FromMinutes(2)); // Cache for 2 minutes
        }

        /// <summary>
        /// Fetch the transaction from the Blockfrost API (with retries/exception handling).
        /// </summary>
        private async Task<Result<Transaction>> FetchTransactionFromApi(string txHash, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Fetching transaction with hash {TxHash} from Blockfrost API", txHash);

                // Create HttpClient
                var client = _httpClientFactory.CreateClient();

                // Create request using the helper
                var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    _appSettings.Blockfrost.BaseUrl,
                    _appSettings.Blockfrost.ApiKey,
                    $"txs/{txHash}");

                // Send the request and get the result
                var result = await BlockfrostHelper.SendBlockfrostRequestAsync<BlockfrostTransactionResponse>(
                    client,
                    httpRequest,
                    _logger,
                    cancellationToken);

                if (result.IsFailed)
                {
                    // If the underlying call fails, bubble up the errors
                    return Result.Fail<Transaction>(result.Errors);
                }

                // Map the API response to your Transaction domain model
                var txResponse = result.Value;
                var transaction = MapToTransaction(txResponse);

                _logger.LogDebug("Successfully retrieved transaction with hash {TxHash}", txHash);
                return Result.Ok(transaction);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching transaction with hash {TxHash}", txHash);
                return Result.Fail<Transaction>($"HTTP error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error occurred while fetching transaction with hash {TxHash}", txHash);
                return Result.Fail<Transaction>($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching transaction with hash {TxHash}", txHash);
                return Result.Fail<Transaction>($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example mapping method from the BlockfrostTransactionResponse to your domain Transaction model.
        /// Adjust the properties to match your internal <see cref="Transaction"/> entity.
        /// </summary>
        private Transaction MapToTransaction(BlockfrostTransactionResponse blockfrostTx)
        {
            return new Transaction
            {
                id = 0, // TODO: Typically not used in the API response
                hash = PrismEncoding.HexToByteArray(blockfrostTx.Hash),
                block_index = blockfrostTx.Index,
                fee = decimal.TryParse(blockfrostTx.Fees, out var fee) ? fee : 0,
                size = blockfrostTx.Size
            };
        }
    }
}
