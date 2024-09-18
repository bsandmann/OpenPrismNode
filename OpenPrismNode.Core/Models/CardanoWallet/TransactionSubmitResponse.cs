using System.Text.Json.Serialization;

public class TransactionSubmitResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}