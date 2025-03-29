namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a template for requesting a verification method.
    /// Used in options.requestVerificationMethod.
    /// </summary>
    public class RegistrarVerificationMethodTemplate
    {
        /// <summary>
        /// The desired identifier fragment (e.g., "#key-1"). Required.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The verification method type. Must be "JsonWebKey2020". Required.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The desired controller DID. Optional.
        /// </summary>
        [JsonPropertyName("controller")]
        public string? Controller { get; set; }

        /// <summary>
        /// The verification method purpose. Must contain at least one value from the allowed list. Required.
        /// Allowed values: "authentication", "assertionMethod", "keyAgreement", "capabilityInvocation", "capabilityDelegation"
        /// </summary>
        [JsonPropertyName("purpose")]
        public List<string>? Purpose { get; set; }
        
        /// <summary>
        /// The cryptographic curve used. Required.
        /// Allowed values: "secp256k1", "Ed25519", "X25519"
        /// </summary>
        [JsonPropertyName("curve")]
        public string? Curve { get; set; }
    }
}