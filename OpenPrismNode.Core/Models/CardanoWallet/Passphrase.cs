using System.Text.Json.Serialization;

public class Passphrase
{
    [JsonPropertyName("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }
}