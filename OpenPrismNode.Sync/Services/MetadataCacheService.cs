using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;
using OpenPrismNode.Sync.Implementations.Blockfrost;

namespace OpenPrismNode.Sync.Services
{
    /// <summary>
    /// Implementation of API transaction cache operations
    /// </summary>
    public class MetadataCacheService : IMetadataCacheService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MetadataCacheService> _logger;
        private readonly AppSettings _appSettings;
        private readonly IAppCache _cache;

        public MetadataCacheService(
            IHttpClientFactory httpClientFactory,
            ILogger<MetadataCacheService> logger,
            IOptions<AppSettings> appSettings,
            IAppCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _cache = cache;
        }

        /// <summary>
        /// Rebuilds the cache for API transactions
        /// </summary>
        public async Task<Result> RebuildCacheAsync(int currentApiBlockTip, CancellationToken cancellationToken = default)
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

                // List of all transaction hashes
                var transactionHashes = transactionMetadataWrappers.Select(p => new TransactionBlockWrapper(p.txHash, null)).ToList();
                _cache.Add(CacheKeys.TransactionList_with_Metadata, transactionHashes, TimeSpan.MaxValue);
                _cache.Add(CacheKeys.TipOfMetadataCacheUpdate, currentApiBlockTip, TimeSpan.MaxValue);

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
        /// Rolls back the cache for API transactions
        /// </summary>
        public async Task<Result> RollbackCacheAsync(int blocksRolledBack, int currentApiBlockTip, CancellationToken cancellationToken = default)
        {
            try
            {
                // Each block can have up to 100 items, and each page can have up to 100 items.
                // We'll fetch exactly 'blocksRolledBack' pages in descending order.
                int pagesToFetch = blocksRolledBack;

                _logger.LogInformation(
                    "Rolling back the cache. Blocks rolled back: {BlocksRolledBack}, Pages to fetch: {PagesToFetch}",
                    blocksRolledBack, pagesToFetch
                );

                var client = _httpClientFactory.CreateClient();

                // Tracks every TxHash found in the rolled-back range (for possible removal).
                var rolledBackTxHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // Tracks every TxHash we re-fetch (hence still valid).
                var newlyFetchedTxHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int page = 1; page <= pagesToFetch; page++)
                {
                    var request = BlockfrostHelper.CreateBlockfrostRequest(
                        baseUrl: _appSettings.Blockfrost.BaseUrl,
                        apiKey: _appSettings.Blockfrost.ApiKey,
                        endpoint: $"/metadata/txs/labels/{_appSettings.MetadataKey}",
                        page: page,
                        count: 100,
                        orderDesc: true // descending
                    );

                    var txInfoResult = await BlockfrostHelper
                        .SendBlockfrostRequestAsync<List<BlockfrostTransactionMetadataResponse>>(
                            client,
                            request,
                            _logger,
                            cancellationToken
                        );

                    if (txInfoResult.IsFailed)
                    {
                        // If we got "Resource not found" after the first page, just break.
                        if (txInfoResult.Errors.FirstOrDefault() is not null
                            && txInfoResult.Errors.First().Message.Equals("Resource not found")
                            && page != 1)
                        {
                            break;
                        }

                        return Result.Fail("Blockfrost request failed during rollback");
                    }

                    var currentPageData = txInfoResult.Value;
                    if (currentPageData == null || !currentPageData.Any())
                    {
                        // No more data
                        break;
                    }

                    // Overwrite cache for each transaction found in this page
                    foreach (var txData in currentPageData)
                    {
                        // Add to our "rolled back range" set
                        rolledBackTxHashes.Add(txData.TxHash);

                        // Parse metadata
                        var transactionMetadataWrappers = new List<TransactionMetadataWrapper>();
                        ReadTransactionMetadata(
                            new List<BlockfrostTransactionMetadataResponse> { txData },
                            transactionMetadataWrappers
                        );

                        // Overwrite in the cache
                        string cacheKey = string.Concat(CacheKeys.MetadataFromPrismTransaction, txData.TxHash);
                        foreach (var transactionMetadataWrapper in transactionMetadataWrappers)
                        {
                            _cache.Add(
                                key: cacheKey,
                                transactionMetadataWrapper,
                                TimeSpan.MaxValue
                            );

                            // This TxHash is still valid.
                            newlyFetchedTxHashes.Add(transactionMetadataWrapper.txHash);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // >>> Final Merge <<<
                // We now remove invalidated TxHashes from the big list, then add the newly fetched ones.
                MergeRolledBackTransactions(rolledBackTxHashes, newlyFetchedTxHashes, currentApiBlockTip);

                return Result.Ok();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred during rollback from Blockfrost API");
                return Result.Fail($"HTTP error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error occurred during rollback from Blockfrost API");
                return Result.Fail($"JSON parsing error: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation canceled during rollback process.");
                return Result.Fail("Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during rollback from Blockfrost API");
                return Result.Fail($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the cache for API transactions
        /// </summary>
        public async Task<Result> UpdateCacheAsync(int currentApiBlockTip, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Updating the transaction metadata cache in descending order.");

                var client = _httpClientFactory.CreateClient();

                int page = 1;
                bool keepFetching = true;

                // Accumulate newly fetched TxHashes
                var newlyFetchedTxHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (keepFetching)
                {
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

                    if (txInfoResult.IsFailed)
                    {
                        // If "Resource not found" after page 1, break
                        if (txInfoResult.Errors.FirstOrDefault() is not null
                            && txInfoResult.Errors.First().Message.Equals("Resource not found")
                            && page != 1)
                        {
                            break;
                        }
                        return Result.Fail("Blockfrost request failed");
                    }

                    var currentPageData = txInfoResult.Value;

                    if (currentPageData == null || !currentPageData.Any())
                    {
                        // No data => done
                        break;
                    }

                    // Iterate through the transactions in the current page
                    foreach (var txData in currentPageData)
                    {
                        string cacheKey = string.Concat(CacheKeys.MetadataFromPrismTransaction, txData.TxHash);

                        // If we already have this transaction in the cache, stop here
                        if (_cache.TryGetValue<TransactionMetadataWrapper>(cacheKey, out _))
                        {
                            keepFetching = false;
                            break;
                        }

                        // Otherwise parse it
                        var transactionMetadataWrappers = new List<TransactionMetadataWrapper>();
                        ReadTransactionMetadata(
                            new List<BlockfrostTransactionMetadataResponse> { txData },
                            transactionMetadataWrappers
                        );

                        // Save to cache
                        foreach (var transactionMetadataWrapper in transactionMetadataWrappers)
                        {
                            _cache.Add(
                                key: cacheKey,
                                transactionMetadataWrapper,
                                TimeSpan.MaxValue
                            );
                            newlyFetchedTxHashes.Add(txData.TxHash);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (keepFetching)
                    {
                        page++;
                    }
                }

                // >>> Final Merge <<<
                MergeNewTransactions(newlyFetchedTxHashes, currentApiBlockTip);

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

        /// <summary>
        /// Adds the block number of the last metadata cache update
        /// </summary>
        public void UpdateBlockNoOfMetadataCacheUpdate(int blockNo)
        {
            _cache.Add(CacheKeys.BlockNoOfMetadataCacheUpdate, blockNo);
        }

        /// <summary>
        /// Merges the newly fetched transactions (still valid) into the global list
        /// and removes transactions that appear in the rolled-back range but were not refetched.
        /// </summary>
        /// <param name="rolledBackTxHashes">All TxHashes in the rollback range.</param>
        /// <param name="newlyFetchedTxHashes">Valid TxHashes in that range.</param>
        private void MergeRolledBackTransactions(HashSet<string> rolledBackTxHashes, HashSet<string> newlyFetchedTxHashes, int currentApiBlockTip)
        {
            // Get existing list of transaction hashes from the cache
            var hasExistingList = _cache.TryGetValue<List<TransactionBlockWrapper>>(
                CacheKeys.TransactionList_with_Metadata,
                out var transactionHashes
            );

            if (!hasExistingList || transactionHashes is null)
            {
                transactionHashes = new List<TransactionBlockWrapper>();
            }

            // 1) Remove invalidated transactions in the rolled-back range
            //    (i.e., was in that range but did NOT reappear in the new fetch)
            transactionHashes.RemoveAll(t =>
                rolledBackTxHashes.Contains(t.TxHash) &&
                !newlyFetchedTxHashes.Contains(t.TxHash)
            );

            // 2) Add newly fetched transactions, skipping duplicates
            foreach (var txHash in newlyFetchedTxHashes)
            {
                bool alreadyInList = transactionHashes.Any(t => t.TxHash.Equals(txHash, StringComparison.OrdinalIgnoreCase));
                if (!alreadyInList)
                {
                    transactionHashes.Add(new TransactionBlockWrapper(txHash, null));
                }
            }

            // Store updated list in cache
            _cache.Add(CacheKeys.TransactionList_with_Metadata, transactionHashes, TimeSpan.MaxValue);
            _cache.Add(CacheKeys.TipOfMetadataCacheUpdate, currentApiBlockTip, TimeSpan.MaxValue);
        }

        /// <summary>
        /// Adds the newly fetched transactions to the global list (if not already present).
        /// </summary>
        /// <param name="newlyFetchedTxHashes">Set of new TxHashes.</param>
        private void MergeNewTransactions(HashSet<string> newlyFetchedTxHashes, int currentApiBlockTip)
        {
            var hasTransactionHashes = _cache.TryGetValue<List<TransactionBlockWrapper>>(
                CacheKeys.TransactionList_with_Metadata,
                out var transactionHashes
            );
            if (!hasTransactionHashes || transactionHashes is null)
            {
                transactionHashes = new List<TransactionBlockWrapper>();
            }

            // Add newly fetched TxHashes, skipping duplicates
            foreach (var txHash in newlyFetchedTxHashes)
            {
                if (!transactionHashes.Any(t => t.TxHash.Equals(txHash, StringComparison.OrdinalIgnoreCase)))
                {
                    transactionHashes.Add(new TransactionBlockWrapper(txHash, null));
                }
            }

            _cache.Add(CacheKeys.TransactionList_with_Metadata, transactionHashes, TimeSpan.MaxValue);
            _cache.Add(CacheKeys.TipOfMetadataCacheUpdate, currentApiBlockTip, TimeSpan.MaxValue);
        }

        private void ReadTransactionMetadata(List<BlockfrostTransactionMetadataResponse> txResponse, List<TransactionMetadataWrapper> transactionMetadataWrappers)
        {
            foreach (var transactionMetadataResponse in txResponse)
            {
                try
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

                                    // Check for duplicate transaction hash
                                    if (transactionMetadataWrappers.Any(p => p.txHash == transactionMetadataResponse.TxHash))
                                    {
                                        // Log this instead of throwing an exception to make processing more resilient
                                        _logger.LogWarning($"Duplicate transaction hash found in metadata response: {transactionMetadataResponse.TxHash}");
                                        continue;
                                    }

                                    transactionMetadataWrappers.Add(new TransactionMetadataWrapper(transactionMetadataResponse.TxHash, transactionJson));
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    // Log the error but continue processing other transactions
                    _logger.LogError(ex, $"JSON error processing transaction {transactionMetadataResponse.TxHash}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other transactions
                    _logger.LogError(ex, $"Unexpected error processing transaction {transactionMetadataResponse.TxHash}: {ex.Message}");
                }
            }
        }
    }
}