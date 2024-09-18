using System.Text.Json.Serialization;

public class Depth
{
    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}