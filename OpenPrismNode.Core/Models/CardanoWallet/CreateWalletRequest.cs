using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class CreateWalletRequest
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("mnemonic_sentence")]
    public string[] MnemonicSentence { get; set; }

    [JsonPropertyName("passphrase")] public string Passphrase { get; set; }

    [JsonPropertyName("address_pool_gap")] public int AddressPoolGap { get; set; } = 20;

    [JsonPropertyName("restoration_mode")] public string? RestorationMode { get; set; }
}