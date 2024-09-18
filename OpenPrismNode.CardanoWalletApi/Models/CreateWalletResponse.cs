using System.Text.Json.Serialization;

public class CreateWalletResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("address_pool_gap")]
    public int AddressPoolGap { get; set; }

    [JsonPropertyName("balance")]
    public Balance Balance { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("passphrase")]
    public Passphrase Passphrase { get; set; }

    [JsonPropertyName("state")]
    public State State { get; set; }

    [JsonPropertyName("tip")]
    public Tip Tip { get; set; }
}