using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionSignRequest
{
    [JsonPropertyName("passphrase")]
    public string Passphrase { get; set; }

    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "base16";
}