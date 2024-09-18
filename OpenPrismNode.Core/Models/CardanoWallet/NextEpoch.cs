using System.Text.Json.Serialization;

public class NextEpoch
{
    [JsonPropertyName("epoch_number")]
    public long EpochNumber { get; set; }

    [JsonPropertyName("epoch_start_time")]
    public DateTime EpochStartTime { get; set; }
}