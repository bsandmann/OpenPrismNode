using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class ExpiresAt
{
    [JsonPropertyName("absolute_slot_number")]
    public long AbsoluteSlotNumber { get; set; }

    [JsonPropertyName("epoch_number")]
    public long EpochNumber { get; set; }

    [JsonPropertyName("slot_number")]
    public long SlotNumber { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
}