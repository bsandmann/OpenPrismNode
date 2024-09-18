using System.Text.Json.Serialization;

public class Payment
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("amount")]
    public Amount Amount { get; set; }

    [JsonPropertyName("assets")]
    public List<Asset> Assets { get; set; } = new();
}