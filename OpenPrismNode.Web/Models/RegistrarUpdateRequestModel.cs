using System.Text.Json.Serialization;

namespace OpenPrismNode.Web.Models;

using Core.Commands.Registrar;

/// <summary>
/// DTO for the Update DID request body.
/// </summary>
public class RegistrarUpdateRequestModel
{
    /// <summary>
    /// Options for the update process.
    /// </summary>
    [JsonPropertyName("options")]
    public RegistrarOptions? Options { get; set; }

    /// <summary>
    /// Secrets (like private keys) needed for authorization.
    /// </summary>
    [JsonPropertyName("secret")]
    public RegistrarSecret? Secret { get; set; }

    /// <summary>
    /// Specifies the update operation(s). Defaults to ["setDidDocument"] if absent.
    /// E.g., ["setDidDocument"], ["addToDidDocument", "removeFromDidDocument"].
    /// </summary>
    [JsonPropertyName("didDocumentOperation")]
    public List<string>? DidDocumentOperation { get; set; }

    /// <summary>
    /// The DID document content or changes, structure depends on didDocumentOperation.
    /// For "setDidDocument", it's an array with one element: the new document.
    /// For "add/remove", it's an array matching the operations with changes. Optional.
    /// </summary>
    [JsonPropertyName("didDocument")]
    public List<RegistrarDidDocument>? DidDocument { get; set; } // Array for update per spec

    /// <summary>
    /// The jobId for tracking the operation
    /// </summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; } = null!;
}