namespace OpenPrismNode.Grpc.Models;

using System.Text.Json.Serialization;

public record TransactionModel
{
    [JsonPropertyName("c")] 
    public List<string>? Content { get; init; }
    
    [JsonPropertyName("v")] 
    public int Version { get; init; }
}