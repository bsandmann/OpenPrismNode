namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DecodeTransaction;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves transaction metadata from the Blockfrost API using a transaction hash.
/// </summary>
public class GetApiTransactionMetadataHandler : IRequestHandler<GetApiTransactionMetadataRequest, Result<Transaction>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiTransactionMetadataHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionMetadataHandler"/> class.
    /// </summary>
    public GetApiTransactionMetadataHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiTransactionMetadataHandler> logger,
        IOptions<AppSettings> appSettings,
        IAppCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
        _cache = cache;
    }

    /// <summary>
    /// Handles the request to retrieve transaction metadata from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request object containing the transaction hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the transaction data or an error</returns>
    public async Task<Result<Transaction>> Handle(GetApiTransactionMetadataRequest request, CancellationToken cancellationToken)
    {
        // if (string.IsNullOrWhiteSpace(request.TxHash))
        // {
        //     _logger.LogError("Transaction hash is required");
        //     return Result.Fail<Transaction>("Transaction hash is required");
        // }

        var cacheResult = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out BlockfrostTransactionMetadataResponse blockfrostTransactionMetadataResponse);
        if (cacheResult)
        {
            // HIT
        }

        try
        {
            _logger.LogDebug("Fetching transaction metadata for hash {TxHash} from Blockfrost API", request.TxHash);

            // Create HttpClient
            var client = _httpClientFactory.CreateClient();

            // Step 1: Get all metadata transactions for PRISM
            var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                _appSettings.Blockfrost.BaseUrl,
                _appSettings.Blockfrost.ApiKey,
                $"/metadata/txs/labels/{_appSettings.MetadataKey}",
                page: 1,
                count: 100,
                orderDesc: false
            );

            var txInfoResult = await BlockfrostHelper.SendBlockfrostRequestAsync<List<BlockfrostTransactionMetadataResponse>>(
                client,
                httpRequest,
                _logger,
                cancellationToken);

            if (txInfoResult.IsFailed)
            {
                return Result.Fail<Transaction>(txInfoResult.Errors);
            }


            var transactionMetadataWrappers = new List<TransactionMetadataWrapper>();
            var txResponse = txInfoResult.Value;
            if (txResponse.Any())
            {
                ReadTransactionMetadata(txResponse, transactionMetadataWrappers);
            }


            return Result.Ok();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail<Transaction>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail<Transaction>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail<Transaction>($"Unexpected error: {ex.Message}");
        }
    }

    private static void ReadTransactionMetadata(List<BlockfrostTransactionMetadataResponse> txResponse, List<TransactionMetadataWrapper> transactionMetadataWrappers)
    {
        foreach (var transactionMetadataResponse in txResponse)
        {
            if (transactionMetadataResponse.JsonMetadata is not null)
            {
                var hasCProp = transactionMetadataResponse.JsonMetadata.Value.TryGetProperty("c", out var cProp);
                var hasVProp = transactionMetadataResponse.JsonMetadata.Value.TryGetProperty("v", out var vProp);

                if (hasCProp && hasVProp && cProp.ValueKind == JsonValueKind.Array)
                {
                    // Validate that 'c' is an array of strings that can safely be processed
                    var hexStrings = new List<string>();
                    var cIsValid = true;

                    foreach (var element in cProp.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            var str = element.GetString();
                            // Ensure each string starts with '0x' and has length > 2
                            if (str is not null && str.StartsWith("0x") && str.Length > 2)
                            {
                                hexStrings.Add(str);
                            }
                            else
                            {
                                cIsValid = false;
                                break;
                            }
                        }
                        else
                        {
                            cIsValid = false;
                            break;
                        }
                    }

                    // Make sure 'v' is a valid integer and is supported by our handler
                    if (cIsValid && vProp.ValueKind == JsonValueKind.Number && vProp.TryGetInt32(out var version))
                    {
                        if (version == 1)
                        {
                            // Construct JSON that matches TransactionModel
                            var payload = new
                            {
                                c = hexStrings,
                                v = version
                            };

                            // Serialize to JSON
                            var transactionJson = JsonSerializer.Serialize(payload);

                            var hasSametransaction = transactionMetadataWrappers.Any(p => p.txHash == transactionMetadataResponse.TxHash);
                            if (hasSametransaction)
                            {
                                //TODO I might be able to remove this check later
                                throw new Exception("Duplicate transaction hash found in metadata response");
                            }

                            transactionMetadataWrappers.Add(new TransactionMetadataWrapper(transactionMetadataResponse.TxHash, transactionJson));
                        }
                    }
                }
            }
        }
    }

    private record TransactionMetadataWrapper(string txHash, string transactionJson);
}