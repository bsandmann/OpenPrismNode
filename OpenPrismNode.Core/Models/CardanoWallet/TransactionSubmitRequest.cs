using System.Text.Json.Serialization;

public class TransactionSubmitRequest
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }
}