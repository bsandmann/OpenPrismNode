using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class State
{
    [JsonPropertyName("progress")]
    public Progress? Progress { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}