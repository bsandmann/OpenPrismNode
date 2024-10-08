using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class AddressResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("derivation_path")]
    public string[] DerivationPath { get; set; }
}