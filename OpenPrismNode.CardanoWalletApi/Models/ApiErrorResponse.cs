using System.Text.Json.Serialization;

public class ApiErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}