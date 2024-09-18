using System.Text.Json.Serialization;

public class TransactionSignResponse
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }
}