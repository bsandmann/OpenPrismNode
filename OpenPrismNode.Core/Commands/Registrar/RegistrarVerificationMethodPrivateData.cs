namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents private data for a verification method (Internal Secret Mode).
    /// Based on DID Core Verification Method structure but includes private material.
    /// </summary>
    public class RegistrarVerificationMethodPrivateData
    {
        /// <summary>
        /// The identifier of the verification method within the DID document (optional).
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of the verification method (e.g., "JsonWebKey2020"). Required.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        /// <summary>
        /// The DID controller. Should actuallby be required, but doesn not align with the way PRISM DIDs are created.
        /// </summary>
        [JsonPropertyName("controller")]
        public string? Controller { get; set; } = null!;

        /// <summary>
        /// Verification relationships (e.g., "authentication", "assertionMethod"). Optional.
        /// </summary>
        [JsonPropertyName("purpose")]
        public List<string>? Purpose { get; set; }

        /// <summary>
        /// Private key in JWK format. Use Dictionary for flexibility.
        /// </summary>
        [JsonPropertyName("privateKeyJwk")]
        public Dictionary<string, object>? PrivateKeyJwk { get; set; }

        /// <summary>
        /// Private key in Multibase format. Not supported in this implementation.
        /// </summary>
        [JsonPropertyName("privateKeyMultibase")]
        public string? PrivateKeyMultibase { get; set; }

        /// <summary>
        /// Additional property which is not in the spec but needed for PRISM
        /// </summary>
        [JsonPropertyName("curve")]
        public string? Curve { get; set; } = null!;

        // Allow other properties if needed
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }

    }
}