using System.Text.Json.Serialization;

public class NetworkInfo
{
    [JsonPropertyName("protocol_magic")]
    public long ProtocolMagic { get; set; }

    [JsonPropertyName("network_id")]
    public string NetworkId { get; set; }
}