namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

using System;
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
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves the latest block (tip) from the Blockfrost API.
/// This handler is responsible for finding the latest block in the blockchain to determine
/// how far the syncing process needs to go.
/// </summary>
public class GetApiBlockTipHandler : IRequestHandler<GetApiBlockTipRequest, Result<Block>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiBlockTipHandler> _logger;
    private readonly AppSettings _appSettings;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockTipHandler"/> class.
    /// </summary>
    public GetApiBlockTipHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiBlockTipHandler> logger,
        IOptions<AppSettings> appSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    /// <summary>
    /// Handles the request to retrieve the latest block from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request object (not used in this case as we always fetch the latest block)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the latest block or an error</returns>
    public async Task<Result<Block>> Handle(GetApiBlockTipRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching latest block from Blockfrost API");
            
            // Create HttpClient
            var client = _httpClientFactory.CreateClient();
            
            // Create request using the helper
            var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                _appSettings.Blockfrost.BaseUrl,
                _appSettings.Blockfrost.ApiKey,
                "blocks/latest");
            
            // Send the request and get the result
            var result = await BlockfrostHelper.SendBlockfrostRequestAsync<BlockfrostBlockResponse>(
                client,
                httpRequest,
                _logger,
                cancellationToken);
            
            if (result.IsFailed)
            {
                return Result.Fail<Block>(result.Errors);
            }
            
            var blockResponse = result.Value;
            
            // Map the API response to our Block model using the shared mapper
            var block = BlockfrostBlockMapper.MapToBlock(blockResponse);
            
            _logger.LogDebug("Successfully retrieved latest block #{BlockNo} from Blockfrost API", block.block_no);
            return Result.Ok(block);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching latest block from Blockfrost API");
            return Result.Fail<Block>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching latest block from Blockfrost API");
            return Result.Fail<Block>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching latest block from Blockfrost API");
            return Result.Fail<Block>($"Unexpected error: {ex.Message}");
        }
    }
}