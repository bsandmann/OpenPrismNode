using System.Text.Json.Serialization;

public class Amount
{
    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}