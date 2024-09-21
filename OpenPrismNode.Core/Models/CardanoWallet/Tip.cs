using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class Tip
{
    [JsonPropertyName("absolute_slot_number")]
    public int AbsoluteSlotNumber { get; set; }

    [JsonPropertyName("slot_number")]
    public int SlotNumber { get; set; }

    [JsonPropertyName("epoch_number")]
    public int EpochNumber { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("height")]
    public Height Height { get; set; }
}