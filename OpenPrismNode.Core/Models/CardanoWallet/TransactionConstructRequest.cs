using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionConstructRequest
{
    [JsonPropertyName("payments")]
    public List<Payment> Payments { get; set; }

    [JsonPropertyName("withdrawal")]
    public string Withdrawal { get; set; } = "self";

    [JsonPropertyName("metadata")]
    public object Metadata { get; set; }

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "base16";
}