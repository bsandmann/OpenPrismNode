using System.Text.Json.Serialization;

public class UtxoAmount
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; }
}