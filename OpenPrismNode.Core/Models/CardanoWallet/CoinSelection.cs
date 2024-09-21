using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class CoinSelection
{
    [JsonPropertyName("inputs")]
    public List<InputOutput> Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public List<InputOutput> Outputs { get; set; }

    [JsonPropertyName("change")]
    public List<Change> Change { get; set; }

    // Additional properties if needed
}