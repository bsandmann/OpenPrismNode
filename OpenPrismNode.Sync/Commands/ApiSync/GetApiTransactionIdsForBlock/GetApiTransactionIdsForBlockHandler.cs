namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionIdsForBlock;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using GetApiTransactionMetadata;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves all transaction IDs contained within a specific block from the Blockfrost API.
/// </summary>
public class GetApiTransactionIdsForBlockHandler : IRequestHandler<GetApiTransactionIdsForBlockRequest, Result<List<string>>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiTransactionMetadataHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionIdsForBlockHandler"/> class.
    /// </summary>
    public GetApiTransactionIdsForBlockHandler(
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
    /// Handles the request to retrieve all transaction IDs contained within a specific block.
    /// Handles pagination to ensure all transactions are retrieved even for blocks with >100 transactions.
    /// </summary>
    /// <param name="request">The request object containing the block number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing a list of transaction hashes (as strings) or an error</returns>
    public async Task<Result<List<string>>> Handle(GetApiTransactionIdsForBlockRequest request, CancellationToken cancellationToken)
    {
        // Create a cache key using the block number
        string cacheKey = string.Concat(CacheKeys.TransactionIdOfBlockNo, request.BlockNo);

        // Try to get from cache first, if not available, execute the fetch function
        return await _cache.GetOrAddAsync(
            cacheKey,
            async () => await FetchTransactionIdsFromApi(request.BlockNo, cancellationToken),
            TimeSpan.FromMinutes(2)); // Cache for 2 minutes
    }

    private async Task<Result<List<string>>> FetchTransactionIdsFromApi(int blockNo, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug($"Fetching transaction IDs for block #{blockNo} from Blockfrost API");

            // Create HttpClient
            var client = _httpClientFactory.CreateClient();

            // For accumulating results from all pages
            var allTransactionIds = new List<string>();

            // The maximum page size is 100 for Blockfrost API
            const int pageSize = 100;
            var currentPage = 1;
            bool hasMorePages = true;

            // Loop until we've retrieved all pages
            while (hasMorePages)
            {
                _logger.LogDebug($"Fetching page {currentPage} of transaction IDs for block #{blockNo}");

                // Get transactions within the block for the current page
                var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    _appSettings.Blockfrost.BaseUrl,
                    _appSettings.Blockfrost.ApiKey,
                    $"blocks/{blockNo}/txs",
                    page: currentPage,
                    count: pageSize,
                    orderDesc: false
                );

                var pageResult = await BlockfrostHelper.SendBlockfrostRequestAsync<List<string>>(
                    client,
                    httpRequest,
                    _logger,
                    cancellationToken);

                if (pageResult.IsFailed)
                {
                    // If fetching a page fails, don't proceed with more pages
                    return Result.Fail<List<string>>(pageResult.Errors);
                }

                var pageTransactions = pageResult.Value;

                // Add current page results to our accumulated list
                allTransactionIds.AddRange(pageTransactions);

                // Check if we need to fetch more pages
                // If we got exactly the page size, there might be more pages
                hasMorePages = pageTransactions.Count == pageSize;

                // Prepare for next page if needed
                if (hasMorePages)
                {
                    currentPage++;
                }
            }

            _logger.LogDebug($"Retrieved a total of {allTransactionIds.Count} transaction IDs from {currentPage} page(s) for block #{blockNo}");

            // Apply Distinct() to ensure no duplicates (though Blockfrost shouldn't return duplicates,
            // this is a safety measure in case of unexpected API behavior)
            return Result.Ok(allTransactionIds.Distinct().ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching transaction IDs from Blockfrost API");
            return Result.Fail<List<string>>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching transaction IDs from Blockfrost API");
            return Result.Fail<List<string>>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching transaction IDs from Blockfrost API");
            return Result.Fail<List<string>>($"Unexpected error: {ex.Message}");
        }
    }
}