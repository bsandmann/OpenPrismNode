using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class ValidityInterval
{
    [JsonPropertyName("invalid_before")]
    public Amount InvalidBefore { get; set; }

    [JsonPropertyName("invalid_hereafter")]
    public Amount InvalidHereafter { get; set; }
}