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
        /// The desired identifier fragment (e.g., "#key-1"). Optional.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The desired type (e.g., "Ed25519VerificationKey2018"). Optional.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The desired controller DID. Optional.
        /// </summary>
        [JsonPropertyName("controller")]
        public string? Controller { get; set; }

        /// <summary>
        /// The desired verification relationships. Optional.
        /// </summary>
        [JsonPropertyName("purpose")]
        public List<string>? Purpose { get; set; }
    }
}