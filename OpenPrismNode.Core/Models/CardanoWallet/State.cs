using System.Text.Json.Serialization;

public class State
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}