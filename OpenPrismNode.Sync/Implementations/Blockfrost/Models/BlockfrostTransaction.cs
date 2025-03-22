namespace OpenPrismNode.Sync.Implementations.Blockfrost.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a transaction from the Blockfrost API
/// </summary>
public class BlockfrostTransaction
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    
    [JsonPropertyName("block")]
    public string Block { get; set; }
    
    [JsonPropertyName("block_height")]
    public int BlockHeight { get; set; }
    
    [JsonPropertyName("slot")]
    public int Slot { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
}