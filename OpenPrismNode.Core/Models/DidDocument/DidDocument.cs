namespace OpenPrismNode.Core.Models.DidDocument;

using System.Text.Json.Serialization;

public class DidDocument
{
    [JsonPropertyName("@context")] public List<string>? Context { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("verificationMethod")]
    public List<VerificationMethod>? VerificationMethod { get; set; }

    [JsonPropertyName("authentication")] public List<string>? Authentication { get; set; }
    [JsonPropertyName("assertionMethod")] public List<string>? AssertionMethod { get; set; }
    [JsonPropertyName("keyAgreement")] public List<string>? KeyAgreement { get; set; }
    [JsonPropertyName("service")] public List<DidDocumentService>? Service { get; set; }
}