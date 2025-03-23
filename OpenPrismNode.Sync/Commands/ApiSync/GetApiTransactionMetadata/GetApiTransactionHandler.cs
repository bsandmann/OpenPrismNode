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
using GetApiTransactionByHash;
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
public class GetApiTransactionHandler : IRequestHandler<GetApiTransactionRequest, Result<Transaction?>>
{
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiTransactionHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionHandler"/> class.
    /// </summary>
    public GetApiTransactionHandler(
        IMediator mediator,
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiTransactionHandler> logger,
        IOptions<AppSettings> appSettings,
        IAppCache cache)
    {
        _mediator = mediator;
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
    public async Task<Result<Transaction?>> Handle(GetApiTransactionRequest request, CancellationToken cancellationToken)
    {
        // if (string.IsNullOrWhiteSpace(request.TxHash))
        // {
        //     _logger.LogError("Transaction hash is required");
        //     return Result.Fail<Transaction>("Transaction hash is required");
        // }

        var cacheResult = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper);
        if (cacheResult)
        {
            var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
            if (transactionResult.IsFailed)
            {
                return transactionResult.ToResult();
            }

            return Result.Ok<Transaction?>(transactionResult.Value);
        }

        var lastBlockWithUpdate = _cache.TryGetValue(CacheKeys.BlockNoOfMetadataCacheUpdate, out int updatedOnBlockNo);
        if (!lastBlockWithUpdate)
        {
            // We don't find the last block with update, so we need to rebuild the cache
            var cachingResult = await RebuildCache(cancellationToken);
            if (cachingResult.IsFailed)
            {
                return cachingResult;
            }

            _cache.Add(CacheKeys.BlockNoOfMetadataCacheUpdate, request.CurrentBlockNo);

            // Look again in the cache
            var cacheResult2 = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper2);
            if (cacheResult2)
            {
                var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
                if (transactionResult.IsFailed)
                {
                    return transactionResult.ToResult();
                }
                return Result.Ok<Transaction?>(transactionResult.Value);
            }

            return Result.Ok();
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo < request.CurrentBlockNo)
        {
            // The cache is outdated, so we need to rebuild it
            var cachingResult = await UpdateCache(cancellationToken);
            if (cachingResult.IsFailed)
            {
                return cachingResult;
            }

            _cache.Add(CacheKeys.BlockNoOfMetadataCacheUpdate, request.CurrentBlockNo);

            // Look again in the cache
            var cacheResult2 = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper2);
            if (cacheResult2)
            {
                var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
                if (transactionResult.IsFailed)
                {
                    return transactionResult.ToResult();
                }
                return Result.Ok<Transaction?>(transactionResult.Value);
            }

            return Result.Ok<Transaction?>(null);
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo == request.CurrentBlockNo)
        {
            // The cache is up to date, but the transaction is not found
            return Result.Ok<Transaction?>(null);
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo > request.CurrentBlockNo)
        {
            // The cache is ahead of the current block
            // This might happen due to a rollback, so we need to rebuild the cache,
            // but with a calculated formular to avoid overcaching
            // We assume that the maximum number of items in a block is 100
            // so the maximum number of pages we have to update is cache is the number of blocks
            // TODO
            return Result.Ok();
        }

        return Result.Ok();
    }

    private async Task<Result> RebuildCache(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching all transaction metadata from Blockfrost API");

            // Create HttpClient once
            var client = _httpClientFactory.CreateClient();

            var allTransactionMetadataResponses = new List<BlockfrostTransactionMetadataResponse>();
            int page = 1;
            bool hasMoreData = true;

            // Keep requesting until no more data is returned
            while (hasMoreData)
            {
                // Create a request for the specific page
                var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    baseUrl: _appSettings.Blockfrost.BaseUrl,
                    apiKey: _appSettings.Blockfrost.ApiKey,
                    endpoint: $"/metadata/txs/labels/{_appSettings.MetadataKey}",
                    page: page,
                    count: 100, // The max number of items per page the API can return
                    orderDesc: false
                );

                var txInfoResult = await BlockfrostHelper
                    .SendBlockfrostRequestAsync<List<BlockfrostTransactionMetadataResponse>>(
                        client,
                        httpRequest,
                        _logger,
                        cancellationToken
                    );

                if (txInfoResult.IsFailed)
                {
                    // If the result is failed, bubble up the error
                    if (txInfoResult.Errors.FirstOrDefault() is not null && txInfoResult.Errors.First().Message.Equals("Resource not found") && page != 1)
                    {
                        hasMoreData = false;
                        continue;
                    }

                    return Result.Fail(txInfoResult.Errors);
                }

                var currentPageData = txInfoResult.Value;
                if (currentPageData == null || !currentPageData.Any())
                {
                    // No more data, exit the loop
                    hasMoreData = false;
                }
                else
                {
                    if (currentPageData.Count < 100)
                    {
                        hasMoreData = false;
                    }
                    // Accumulate the results
                    allTransactionMetadataResponses.AddRange(currentPageData);
                    // Move to the next page
                    page++;
                }

                // (Optional) Check for cancellation in between pages
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Now we have all the transaction metadata responses, parse them
            var transactionMetadataWrappers = new List<TransactionMetadataWrapper>();
            ReadTransactionMetadata(allTransactionMetadataResponses, transactionMetadataWrappers);

            // Add everything to the cache
            foreach (var transactionMetadataWrapper in transactionMetadataWrappers)
            {
                _cache.Add(
                    key: string.Concat(CacheKeys.MetadataFromPrismTransaction, transactionMetadataWrapper.txHash),
                    transactionMetadataWrapper,
                    TimeSpan.MaxValue
                );
            }

            return Result.Ok();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail($"JSON parsing error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation canceled while fetching transaction data.");
            return Result.Fail("Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching transaction data from Blockfrost API");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Update the cache by fetching transaction metadata in descending order
    /// and stopping as soon as an item is encountered that is already in the cache.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result> UpdateCache(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Updating the transaction metadata cache in descending order.");

            var client = _httpClientFactory.CreateClient();

            int page = 1;
            bool keepFetching = true;

            while (keepFetching)
            {
                // Prepare request for descending order
                var request = BlockfrostHelper.CreateBlockfrostRequest(
                    baseUrl: _appSettings.Blockfrost.BaseUrl,
                    apiKey: _appSettings.Blockfrost.ApiKey,
                    endpoint: $"/metadata/txs/labels/{_appSettings.MetadataKey}",
                    page: page,
                    count: 100,
                    orderDesc: true
                );

                var txInfoResult = await BlockfrostHelper
                    .SendBlockfrostRequestAsync<List<BlockfrostTransactionMetadataResponse>>(
                        client,
                        request,
                        _logger,
                        cancellationToken
                    );

                // If the request failed, return an error result
                if (txInfoResult.IsFailed)
                {
                    // If the result is failed, bubble up the error
                    if (txInfoResult.Errors.FirstOrDefault() is not null && txInfoResult.Errors.First().Message.Equals("Resource not found") && page != 1)
                    {
                        keepFetching = false;
                        continue;
                    }

                    return Result.Fail($"Blockfrost request failed");
                }

                var currentPageData = txInfoResult.Value;

                // If no data was returned, we've exhausted all pages
                if (currentPageData == null || !currentPageData.Any())
                {
                    keepFetching = false;
                    break;
                }

                // Iterate through the transactions in the current page
                foreach (var txData in currentPageData)
                {
                    string cacheKey = string.Concat(CacheKeys.MetadataFromPrismTransaction, txData.TxHash);

                    // If we already have this transaction in the cache, stop here
                    if (_cache.TryGetValue<TransactionMetadataWrapper>(cacheKey, out var _))
                    {
                        keepFetching = false;
                        break;
                    }

                    var transactionMetadataWrappers = new List<TransactionMetadataWrapper>();
                    ReadTransactionMetadata(new List<BlockfrostTransactionMetadataResponse>() { txData }, transactionMetadataWrappers);
                    // Otherwise, add new transaction data to cache
                    foreach (var transactionMetadataWrapper in transactionMetadataWrappers)
                    {
                        _cache.Add(
                            key: string.Concat(CacheKeys.MetadataFromPrismTransaction, txData.TxHash),
                            transactionMetadataWrapper,
                            TimeSpan.MaxValue
                        );
                    }

                    // Optional: check for cancellation in the inner loop
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // If we still want to fetch the next page, increment and continue
                if (keepFetching)
                {
                    page++;
                }
            }

            // If we get here, we've either found an existing record or fetched all pages
            return Result.Ok();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while updating transaction data from Blockfrost API");
            return Result.Fail($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while updating transaction data from Blockfrost API");
            return Result.Fail($"JSON parsing error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation canceled while updating transaction data.");
            return Result.Fail("Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating transaction data from Blockfrost API");
            return Result.Fail($"Unexpected error: {ex.Message}");
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

}