using System.Text.Json.Serialization;

public class SyncProgress
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("progress")]
    public Progress Progress { get; set; }
}