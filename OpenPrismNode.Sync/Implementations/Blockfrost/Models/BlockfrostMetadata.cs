namespace OpenPrismNode.Sync.Implementations.Blockfrost.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents transaction metadata from the Blockfrost API
/// </summary>
public class BlockfrostMetadata
{
    [JsonPropertyName("label")]
    public string Label { get; set; }
    
    [JsonPropertyName("json_metadata")]
    public string JsonMetadata { get; set; }
}