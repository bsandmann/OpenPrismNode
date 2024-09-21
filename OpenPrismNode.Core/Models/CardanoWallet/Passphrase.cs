using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Passphrase
{
    [JsonPropertyName("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }
}