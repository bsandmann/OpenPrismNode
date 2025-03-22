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
            
            // Map the API response to our Block model
            var block = MapToBlock(blockResponse);
            
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
    
    /// <summary>
    /// Maps the Blockfrost API response to our internal Block model.
    /// </summary>
    /// <param name="response">The API response containing block data</param>
    /// <returns>A Block object with mapped properties</returns>
    private Block MapToBlock(BlockfrostBlockResponse response)
    {
        return new Block
        {
            // We don't have a direct equivalent for id in the API, so we use a placeholder
            // In a real implementation, you might want to use a different strategy or store this mapping
            id = -1, 
            
            // Convert UNIX timestamp to DateTime
            time = DateTimeOffset.FromUnixTimeSeconds(response.Time).DateTime,
            
            // Map directly from response
            block_no = response.Height,
            epoch_no = response.Epoch,
            tx_count = response.TxCount,
            
            // Convert hex strings to byte arrays
            hash = ConvertHexStringToByteArray(response.Hash),
            
            // No direct mapping for previous_id, use a placeholder
            previous_id = -1,
            
            // Convert hex string to byte array
            previousHash = ConvertHexStringToByteArray(response.PreviousBlock)
        };
    }
    
    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A byte array representing the hexadecimal string</returns>
    private byte[] ConvertHexStringToByteArray(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return new byte[0];

        // Remove "0x" prefix if present
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        // Create byte array
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        
        return bytes;
    }
}