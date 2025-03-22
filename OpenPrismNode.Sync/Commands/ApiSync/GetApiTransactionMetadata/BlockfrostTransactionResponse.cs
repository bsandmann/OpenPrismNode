namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System.Text.Json.Serialization;

/// <summary>
/// Response model for the Blockfrost transaction API endpoint.
/// </summary>
public class BlockfrostTransactionResponse
{
    /// <summary>
    /// Transaction hash (hex encoded)
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Transaction index within the block
    /// </summary>
    [JsonPropertyName("block_index")]
    public int BlockIndex { get; set; }

    /// <summary>
    /// Transaction fee (in Lovelace)
    /// </summary>
    [JsonPropertyName("fees")]
    public string Fees { get; set; } = string.Empty;

    /// <summary>
    /// Transaction size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }
}