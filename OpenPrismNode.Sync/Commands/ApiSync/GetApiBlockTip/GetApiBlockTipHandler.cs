namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

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
using OpenPrismNode.Sync.Implementations.Blockfrost;

/// <summary>
/// Retrieves the latest block (tip) from the Blockfrost API.
/// This handler is responsible for finding the latest block in the blockchain to determine
/// how far the syncing process needs to go.
/// </summary>
public class GetApiBlockTipHandler : IRequestHandler<GetApiBlockTipRequest, Result<Block>>
{
    private readonly BlockfrostApiClient _apiClient;
    private readonly ILogger<GetApiBlockTipHandler> _logger;
    private readonly AppSettings _appSettings;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockTipHandler"/> class.
    /// </summary>
    public GetApiBlockTipHandler(
        BlockfrostApiClient apiClient,
        ILogger<GetApiBlockTipHandler> logger,
        IOptions<AppSettings> appSettings)
    {
        _apiClient = apiClient;
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
            
            // Get the latest block from the Blockfrost API
            var ff = _apiClient.BaseAddress;
            var result = await _apiClient.GetAsync<BlockfrostLatestBlockResponse>("blocks/latest", cancellationToken);
            
            if (result.IsFailed)
            {
                _logger.LogError("Failed to get latest block from Blockfrost API: {Error}", 
                    result.Errors.Count > 0 ? result.Errors[0].Message : "Unknown error");
                return result.ToResult();
            }
            
            // Map the API response to our Block model
            var block = MapToBlock(result.Value);
            
            _logger.LogDebug("Successfully retrieved latest block #{BlockNo} from Blockfrost API", block.block_no);
            return Result.Ok(block);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching latest block from Blockfrost API");
            return Result.Fail($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while fetching latest block from Blockfrost API");
            return Result.Fail($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching latest block from Blockfrost API");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Maps the Blockfrost API response to our internal Block model.
    /// </summary>
    /// <param name="response">The API response containing block data</param>
    /// <returns>A Block object with mapped properties</returns>
    private Block MapToBlock(BlockfrostLatestBlockResponse response)
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

/// <summary>
/// Response model for the Blockfrost latest block API endpoint.
/// </summary>
public class BlockfrostLatestBlockResponse
{
    /// <summary>
    /// Block creation time in UNIX time
    /// </summary>
    public long Time { get; set; }
    
    /// <summary>
    /// Block number
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Block hash (hex encoded)
    /// </summary>
    public string Hash { get; set; } = string.Empty;
    
    /// <summary>
    /// Slot number
    /// </summary>
    public int Slot { get; set; }
    
    /// <summary>
    /// Epoch number
    /// </summary>
    public int Epoch { get; set; }
    
    /// <summary>
    /// Slot within the epoch
    /// </summary>
    public int EpochSlot { get; set; }
    
    /// <summary>
    /// Transaction count
    /// </summary>
    public int TxCount { get; set; }
    
    /// <summary>
    /// Previous block hash (hex encoded)
    /// </summary>
    public string PreviousBlock { get; set; } = string.Empty;
    
    /// <summary>
    /// Next block hash (hex encoded)
    /// </summary>
    public string NextBlock { get; set; } = string.Empty;
}