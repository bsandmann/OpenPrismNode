namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlocksByNumbers;

using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Implementations.Blockfrost;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handler that retrieves a list of consecutive blocks from the Blockfrost API,
/// starting with <see cref="GetApiBlocksByNumbersRequest.FirstBlockNo"/>.
/// </summary>
public class GetApiBlocksByNumbersHandler : IRequestHandler<GetApiBlocksByNumbersRequest, Result<List<Block>>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiBlocksByNumbersHandler> _logger;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlocksByNumbersHandler"/> class.
    /// </summary>
    public GetApiBlocksByNumbersHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiBlocksByNumbersHandler> logger,
        IOptions<AppSettings> appSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// Handles the request to retrieve <paramref name="request.Count"/> blocks,
    /// starting at <paramref name="request.FirstBlockNo"/>, using the
    /// <c>/blocks/{hash_or_number}/next</c> endpoint from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request specifying the starting block and number of blocks to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of blocks from <paramref name="request.FirstBlockNo"/> up to
    /// <c>request.FirstBlockNo + request.Count - 1</c>, subject to chain tip.</returns>
    public async Task<Result<List<Block>>> Handle(GetApiBlocksByNumbersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Count < 1)
            {
                return Result.Fail<List<Block>>("Count must be at least 1.");
            }

            _logger.LogDebug("Fetching {Count} blocks starting at block #{FirstBlockNo}", request.Count, request.FirstBlockNo);

            var client = _httpClientFactory.CreateClient();
            var baseUrl = _appSettings.Blockfrost.BaseUrl;
            var apiKey = _appSettings.Blockfrost.ApiKey;

            // 1) Fetch the single block for FirstBlockNo
            var singleBlockReq = BlockfrostHelper.CreateBlockfrostRequest(
                baseUrl,
                apiKey,
                $"blocks/{request.FirstBlockNo}");

            var singleBlockResult = await BlockfrostHelper.SendBlockfrostRequestAsync<BlockfrostBlockResponse>(
                client,
                singleBlockReq,
                _logger,
                cancellationToken);

            if (singleBlockResult.IsFailed)
            {
                return Result.Fail<List<Block>>(singleBlockResult.Errors);
            }

            // Map that block and add it to the result list
            var blocks = new List<Block>(request.Count);
            var firstBlock = BlockfrostBlockMapper.MapToBlock(singleBlockResult.Value);
            blocks.Add(firstBlock);

            // 2) Fetch subsequent blocks via /blocks/{hash_or_number}/next
            int remaining = request.Count - 1; // we already have the first one
            int page = 1;                      // for pagination

            while (remaining > 0)
            {
                // The API's "count" query param max is 100
                int pageSize = Math.Min(100, remaining);

                var nextBlocksRequest = BlockfrostHelper.CreateBlockfrostRequest(
                    baseUrl,
                    apiKey,
                    $"blocks/{request.FirstBlockNo}/next?count={pageSize}&page={page}");

                var nextBlocksResult = await BlockfrostHelper.SendBlockfrostRequestAsync<List<BlockfrostBlockResponse>>(
                    client,
                    nextBlocksRequest,
                    _logger,
                    cancellationToken);

                if (nextBlocksResult.IsFailed)
                {
                    return Result.Fail<List<Block>>(nextBlocksResult.Errors);
                }

                var nextBlocksList = nextBlocksResult.Value;
                if (nextBlocksList is { Count: > 0 })
                {
                    foreach (var bfBlock in nextBlocksList)
                    {
                        blocks.Add(BlockfrostBlockMapper.MapToBlock(bfBlock));
                    }
                }

                remaining -= pageSize;
                page++;

                // If the API returned fewer than requested, we've hit the chain tip; break early
                if (nextBlocksList.Count < pageSize)
                {
                    break;
                }
            }

            _logger.LogDebug(
                "Successfully fetched {Fetched} blocks starting at #{FirstBlockNo}",
                blocks.Count,
                request.FirstBlockNo);

            return Result.Ok(blocks);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching blocks starting at #{FirstBlockNo}", request.FirstBlockNo);
            return Result.Fail<List<Block>>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching blocks starting at #{FirstBlockNo}", request.FirstBlockNo);
            return Result.Fail<List<Block>>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching blocks starting at #{FirstBlockNo}", request.FirstBlockNo);
            return Result.Fail<List<Block>>($"Unexpected error: {ex.Message}");
        }
    }
}
