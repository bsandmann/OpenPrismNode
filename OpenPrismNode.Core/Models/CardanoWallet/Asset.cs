using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Asset
{
    [JsonPropertyName("policy_id")]
    public string PolicyId { get; set; }

    [JsonPropertyName("asset_name")]
    public string AssetName { get; set; }

    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }
}