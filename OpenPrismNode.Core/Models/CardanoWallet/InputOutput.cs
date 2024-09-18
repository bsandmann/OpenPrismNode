using System.Text.Json.Serialization;

public class InputOutput
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("amount")]
    public Amount Amount { get; set; }

    [JsonPropertyName("assets")]
    public List<Asset> Assets { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("derivation_path")]
    public string[] DerivationPath { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}