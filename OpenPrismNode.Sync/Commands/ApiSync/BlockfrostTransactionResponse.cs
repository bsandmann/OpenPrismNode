namespace OpenPrismNode.Sync.Commands.ApiSync;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the response for a transaction from the Blockfrost API.
/// </summary>
public class BlockfrostTransactionResponse
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    
    [JsonPropertyName("block")]
    public string Block { get; set; }
    
    [JsonPropertyName("block_height")]
    public int BlockHeight { get; set; }
    
    [JsonPropertyName("block_time")]
    public int BlockTime { get; set; }
    
    [JsonPropertyName("slot")]
    public int Slot { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("output_amount")]
    public List<BlockfrostTransactionOutputAmount> OutputAmount { get; set; }
    
    [JsonPropertyName("fees")]
    public string Fees { get; set; }
    
    [JsonPropertyName("deposit")]
    public string Deposit { get; set; }
    
    [JsonPropertyName("size")]
    public int Size { get; set; }
    
    [JsonPropertyName("invalid_before")]
    public string InvalidBefore { get; set; }
    
    [JsonPropertyName("invalid_hereafter")]
    public string InvalidHereafter { get; set; }
    
    [JsonPropertyName("utxo_count")]
    public int UtxoCount { get; set; }
    
    [JsonPropertyName("withdrawal_count")]
    public int WithdrawalCount { get; set; }
    
    [JsonPropertyName("mir_cert_count")]
    public int MirCertCount { get; set; }
    
    [JsonPropertyName("delegation_count")]
    public int DelegationCount { get; set; }
    
    [JsonPropertyName("stake_cert_count")]
    public int StakeCertCount { get; set; }
    
    [JsonPropertyName("pool_update_count")]
    public int PoolUpdateCount { get; set; }
    
    [JsonPropertyName("pool_retire_count")]
    public int PoolRetireCount { get; set; }
    
    [JsonPropertyName("asset_mint_or_burn_count")]
    public int AssetMintOrBurnCount { get; set; }
    
    [JsonPropertyName("redeemer_count")]
    public int RedeemerCount { get; set; }
    
    [JsonPropertyName("valid_contract")]
    public bool ValidContract { get; set; }
}