namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

using System.Text.Json.Serialization;

/// <summary>
/// Response model for the Blockfrost latest block API endpoint.
/// </summary>
public class BlockfrostLatestBlockResponse
{
    /// <summary>
    /// Block creation time in UNIX time
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; }
    
    /// <summary>
    /// Block number
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    /// <summary>
    /// Block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
    
    /// <summary>
    /// Slot number
    /// </summary>
    [JsonPropertyName("slot")]
    public int Slot { get; set; }
    
    /// <summary>
    /// Epoch number
    /// </summary>
    [JsonPropertyName("epoch")]
    public int Epoch { get; set; }
    
    /// <summary>
    /// Slot within the epoch
    /// </summary>
    [JsonPropertyName("epoch_slot")]
    public int EpochSlot { get; set; }
    
    /// <summary>
    /// Slot leader (pool ID)
    /// </summary>
    [JsonPropertyName("slot_leader")]
    public string SlotLeader { get; set; } = string.Empty;
    
    /// <summary>
    /// Block size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }
    
    /// <summary>
    /// Transaction count
    /// </summary>
    [JsonPropertyName("tx_count")]
    public int TxCount { get; set; }
    
    /// <summary>
    /// Total output in Lovelace
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;
    
    /// <summary>
    /// Total fees in Lovelace
    /// </summary>
    [JsonPropertyName("fees")]
    public string Fees { get; set; } = string.Empty;
    
    /// <summary>
    /// VRF key of the block
    /// </summary>
    [JsonPropertyName("block_vrf")]
    public string BlockVrf { get; set; } = string.Empty;
    
    /// <summary>
    /// Operational certificate
    /// </summary>
    [JsonPropertyName("op_cert")]
    public string OpCert { get; set; } = string.Empty;
    
    /// <summary>
    /// Operational certificate counter
    /// </summary>
    [JsonPropertyName("op_cert_counter")]
    public string OpCertCounter { get; set; } = string.Empty;
    
    /// <summary>
    /// Previous block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("previous_block")]
    public string PreviousBlock { get; set; } = string.Empty;
    
    /// <summary>
    /// Next block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("next_block")]
    public string? NextBlock { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of confirmations
    /// </summary>
    [JsonPropertyName("confirmations")]
    public int Confirmations { get; set; }
}