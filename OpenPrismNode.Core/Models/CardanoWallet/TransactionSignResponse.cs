using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionSignResponse
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }
}