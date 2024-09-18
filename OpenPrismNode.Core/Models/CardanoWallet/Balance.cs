using System.Text.Json.Serialization;

public class Balance
{
    [JsonPropertyName("available")]
    public Amount Available { get; set; }

    [JsonPropertyName("reward")]
    public Amount Reward { get; set; }

    [JsonPropertyName("total")]
    public Amount Total { get; set; }
}