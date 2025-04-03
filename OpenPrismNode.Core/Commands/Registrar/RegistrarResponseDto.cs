namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Text.Json.Serialization;
    using Models.DidDocument;

    /// <summary>
    /// Common response structure for DID Registrar operations.
    /// </summary>
    public class RegistrarResponseDto
    {
        /// <summary>
        /// Identifier for tracking an ongoing, multi-step operation. Null if finished or failed immediately.
        /// </summary>
        [JsonPropertyName("jobId")]
        public string? JobId { get; set; }

        /// <summary>
        /// The current state of the DID and the registration process. Required.
        /// </summary>
        [JsonPropertyName("didState")]
        public RegistrarDidState DidState { get; set; } = null!;

        /// <summary>
        /// Metadata about the registration process itself (e.g., timings, network info). Optional.
        /// </summary>
        [JsonPropertyName("didRegistrationMetadata")]
        public RegistrarDidRegistrationMetadata? DidRegistrationMetadata { get; set; }

        /// <summary>
        /// Metadata about the DID document (e.g., version, transaction IDs). Optional.
        /// </summary>
        [JsonPropertyName("didDocumentMetadata")]
        public DidDocumentMetadata? DidDocumentMetadata { get; set; }
    }
}