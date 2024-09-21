using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Height
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}