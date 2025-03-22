namespace OpenPrismNode.Sync.Implementations.Blockfrost.Models;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a block from the Blockfrost API
/// </summary>
public class BlockfrostBlock
{
    [JsonPropertyName("time")]
    public long TimeUnix { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    
    [JsonPropertyName("slot")]
    public int Slot { get; set; }
    
    [JsonPropertyName("epoch")]
    public int Epoch { get; set; }
    
    [JsonPropertyName("epoch_slot")]
    public int EpochSlot { get; set; }
    
    [JsonPropertyName("previous_block")]
    public string PreviousBlock { get; set; }
    
    [JsonPropertyName("tx_count")]
    public int TxCount { get; set; }
}