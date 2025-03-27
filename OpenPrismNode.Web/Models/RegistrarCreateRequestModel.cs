using System.Text.Json.Serialization;

namespace OpenPrismNode.Web.Models;

using Core.Commands.Registrar;

public class RegistrarCreateRequestModel
{
    /// <summary>
    /// The DID method to use (e.g., "prism", "key", "web"). Required.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    /// <summary>
    /// The specific DID to create (optional, usage depends on DID method).
    /// </summary>
    [JsonPropertyName("did")]
    public string? Did { get; set; }

    /// <summary>
    /// Options for the creation process.
    /// </summary>
    [JsonPropertyName("options")]
    public RegistrarOptions? Options { get; set; }

    /// <summary>
    /// Secrets (like private keys) provided by the client for the operation.
    /// Required for Internal Secret Mode if providing pre-existing keys.
    /// </summary>
    [JsonPropertyName("secret")]
    public RegistrarSecret? Secret { get; set; }

    /// <summary>
    /// The initial DID document content. Required for create.
    /// Should be a single JSON object representing the DID Document.
    /// </summary>
    [JsonPropertyName("didDocument")]
    public RegistrarDidDocument DidDocument { get; set; } = null!;
}