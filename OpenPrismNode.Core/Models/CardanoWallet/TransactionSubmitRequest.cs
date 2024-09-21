using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionSubmitRequest
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }
}