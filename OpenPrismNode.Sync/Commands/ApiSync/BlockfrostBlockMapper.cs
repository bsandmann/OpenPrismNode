namespace OpenPrismNode.Sync.Commands.ApiSync;

using System;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

/// <summary>
/// Utility class for mapping Blockfrost API responses to internal models.
/// </summary>
public static class BlockfrostBlockMapper
{
    /// <summary>
    /// Maps the Blockfrost API response to our internal Block model.
    /// </summary>
    /// <param name="response">The API response containing block data</param>
    /// <returns>A Block object with mapped properties</returns>
    public static Block MapToBlock(BlockfrostBlockResponse response)
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
    public static byte[] ConvertHexStringToByteArray(string hex)
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