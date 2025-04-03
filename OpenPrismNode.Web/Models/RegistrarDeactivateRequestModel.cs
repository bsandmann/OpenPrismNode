using System.Text.Json.Serialization;

namespace OpenPrismNode.Web.Models;

using Core.Commands.Registrar;

/// <summary>
/// DTO for the Deactivate DID request body.
/// </summary>
public class RegistrarDeactivateRequestModel
{
    /// <summary>
    /// Options for the deactivation process.
    /// </summary>
    [JsonPropertyName("options")]
    public RegistrarOptions? Options { get; set; }

    /// <summary>
    /// Secrets (like private keys) needed for authorization.
    /// </summary>
    [JsonPropertyName("secret")]
    public RegistrarSecret? Secret { get; set; }

    /// <summary>
    /// The jobId for tracking the operation
    /// </summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; } = null!;
}