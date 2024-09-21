using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Depth
{
    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}