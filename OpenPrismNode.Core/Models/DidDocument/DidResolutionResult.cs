namespace OpenPrismNode.Core.Models.DidDocument;

using System.Text.Json.Serialization;

public class DidResolutionResult
{
    public const string DidResolutionResultContext = "https://www.w3.org/ns/did-resolution/v1";
    
    /// <summary>
    /// As seen here https://w3c-ccg.github.io/did-resolution/#did-resolution-result
    /// </summary>
    [JsonPropertyName("@context")]
    public string Context { get; init; }

    /// <summary>
    /// As described in DID-Core spec: https://www.w3.org/TR/did-core/#core-properties
    /// </summary>
    [JsonPropertyName("didDocument")]
    public required DidDocument DidDocument { get; set; } = new();

    /// <summary>
    /// https://www.w3.org/TR/did-core/#did-resolution-metadata
    /// See also the spec: https://www.w3.org/TR/did-spec-registries/#did-resolution-metadata
    /// Only used when running through a Universal Resolver
    /// </summary>
    public DidResolutionMetadata? DidResolutionMetadata { get; init; } = new();

    /// <summary>
    /// https://www.w3.org/TR/did-core/#did-document-metadata
    /// See also the spec: https://www.w3.org/TR/did-spec-registries/#did-document-metadata
    /// </summary>
    [JsonPropertyName("didDocumentMetadata")]
    public required DidDocumentMetadata DidDocumentMetadata { get; init; } = new(); 
}