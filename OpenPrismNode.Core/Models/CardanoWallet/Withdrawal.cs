using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Withdrawal
{
    [JsonPropertyName("stake_address")]
    public string StakeAddress { get; set; }

    [JsonPropertyName("amount")]
    public Amount Amount { get; set; }
}