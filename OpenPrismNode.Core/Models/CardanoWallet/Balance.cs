using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Balance
{
    [JsonPropertyName("available")]
    public Amount Available { get; set; }

    [JsonPropertyName("reward")]
    public Amount Reward { get; set; }

    [JsonPropertyName("total")]
    public Amount Total { get; set; }
}