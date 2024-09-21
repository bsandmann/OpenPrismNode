using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class SyncProgress
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("progress")]
    public Progress Progress { get; set; }
}