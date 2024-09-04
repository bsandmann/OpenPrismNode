namespace OpenPrismNode.Core.Models.DidDocument;

using System.Text.Json.Serialization;

[JsonConverter(typeof(ServiceEndpointConverter))]
public class DidDocumentService
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    public List<string>? ServiceEndpointStringList { get; set; }

    public string? ServiceEndpointString { get; set; }
    public Dictionary<string, object>? ServiceEndpointObject { get; set; }
}