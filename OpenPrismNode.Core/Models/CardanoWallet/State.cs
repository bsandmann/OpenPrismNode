using System.Text.Json.Serialization;

public class State
{
    [JsonPropertyName("progress")]
    public Progress Progress { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}