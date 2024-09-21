using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionSubmitResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}