using System.Text.Json.Serialization;

public class TransactionConstructResponse
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; }

    [JsonPropertyName("coin_selection")]
    public CoinSelection CoinSelection { get; set; }

    [JsonPropertyName("fee")]
    public Amount Fee { get; set; }
}