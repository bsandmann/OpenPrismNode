namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockInEpoch;

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves a specific block by epoch and slot from the Blockfrost API.
/// </summary>
public class GetApiBlockInEpochHandler : IRequestHandler<GetApiBlockInEpochRequest, Result<Block?>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiBlockInEpochHandler> _logger;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockInEpochHandler"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory used to create HttpClient instances</param>
    /// <param name="logger">The logger</param>
    /// <param name="appSettings">Application settings</param>
    public GetApiBlockInEpochHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiBlockInEpochHandler> logger,
        IOptions<AppSettings> appSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// Handles the request to retrieve a block by epoch number and slot number from the Blockfrost API.
    /// If the block doesn't exist (HTTP 404), we return a success result with a null Block.
    /// </summary>
    /// <param name="request">The request containing the epoch number and slot number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the requested block or null if not found (404), or an error otherwise.</returns>
    public async Task<Result<Block?>> Handle(GetApiBlockInEpochRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Fetching block for epoch {EpochNumber}, slot {SlotNumber} from Blockfrost API",
                request.EpochNumber,
                request.SlotNumber);

            // Create HttpClient
            var client = _httpClientFactory.CreateClient();

            // Build the Blockfrost endpoint for the specific block in an epoch/slot
            var endpointPath = $"blocks/epoch/{request.EpochNumber}/slot/{request.SlotNumber}";

            // Create request using the helper
            var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                _appSettings.Blockfrost.BaseUrl,
                _appSettings.Blockfrost.ApiKey,
                endpointPath);

            // Send the request and get the result
            var blockfrostResult = await BlockfrostHelper.SendBlockfrostRequestAsync<BlockfrostBlockResponse>(
                client,
                httpRequest,
                _logger,
                cancellationToken);

            // If the request itself failed
            if (blockfrostResult.IsFailed)
            {
                // Check if the error is "404 - Not Found",
                // which means no block exists at this slot => return null instead of an error.
                bool is404 = blockfrostResult.Errors.Exists(e =>
                    e.Message.Contains("404") ||
                    e.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase));

                if (is404)
                {
                    // Return success with null block, effectively ignoring the 404
                    return Result.Ok<Block?>(null);
                }

                // Otherwise, return the actual errors
                return Result.Fail<Block?>(blockfrostResult.Errors);
            }

            // Request succeeded; map the API response to our internal Block model
            var blockResponse = blockfrostResult.Value;
            var block = BlockfrostBlockMapper.MapToBlock(blockResponse);

            _logger.LogDebug(
                "Successfully retrieved block at epoch #{EpochNo}, slot #{SlotNo}",
                block.epoch_no,
                request.SlotNumber);

            return Result.Ok<Block?>(block);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error occurred while fetching block for epoch {EpochNumber}, slot {SlotNumber}",
                request.EpochNumber,
                request.SlotNumber);

            return Result.Fail<Block?>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "JSON parsing error occurred while fetching block for epoch {EpochNumber}, slot {SlotNumber}",
                request.EpochNumber,
                request.SlotNumber);

            return Result.Fail<Block?>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while fetching block for epoch {EpochNumber}, slot {SlotNumber}",
                request.EpochNumber,
                request.SlotNumber);

            return Result.Fail<Block?>($"Unexpected error: {ex.Message}");
        }
    }
}
