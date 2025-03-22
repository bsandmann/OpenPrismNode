namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockByNumber;

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
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves a specific block by its number from the Blockfrost API.
/// This handler fetches a block with a specific height from the blockchain.
/// </summary>
public class GetApiBlockByNumberHandler : IRequestHandler<GetApiBlockByNumberRequest, Result<Block>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiBlockByNumberHandler> _logger;
    private readonly AppSettings _appSettings;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockByNumberHandler"/> class.
    /// </summary>
    public GetApiBlockByNumberHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiBlockByNumberHandler> logger,
        IOptions<AppSettings> appSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    /// <summary>
    /// Handles the request to retrieve a block by its number from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request containing the block number to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the requested block or an error</returns>
    public async Task<Result<Block>> Handle(GetApiBlockByNumberRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching block #{BlockNumber} from Blockfrost API", request.BlockNumber);
            
            // Create HttpClient
            var client = _httpClientFactory.CreateClient();
            
            // Create request using the helper
            var httpRequest = BlockfrostHelper.CreateBlockfrostRequest(
                _appSettings.Blockfrost.BaseUrl,
                _appSettings.Blockfrost.ApiKey,
                $"blocks/{request.BlockNumber}");
            
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
            
            _logger.LogDebug("Successfully retrieved block #{BlockNo} from Blockfrost API", block.block_no);
            return Result.Ok(block);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching block #{BlockNumber} from Blockfrost API", request.BlockNumber);
            return Result.Fail<Block>($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching block #{BlockNumber} from Blockfrost API", request.BlockNumber);
            return Result.Fail<Block>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching block #{BlockNumber} from Blockfrost API", request.BlockNumber);
            return Result.Fail<Block>($"Unexpected error: {ex.Message}");
        }
    }
}