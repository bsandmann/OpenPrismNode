namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents secrets used in DID registration operations, primarily for Internal Secret Mode.
    /// </summary>
    public class RegistrarSecret
    {
        /// <summary>
        /// List of verification methods including private key material (Internal Mode)
        /// or public key material (Client Mode - not used here).
        /// </summary>
        [JsonPropertyName("verificationMethod")]
        public List<RegistrarVerificationMethodPrivateData>? VerificationMethod { get; set; }

        // Placeholder for other potential secrets like seeds, passwords, etc.
        [JsonExtensionData]
        public Dictionary<string, object>? OtherSecrets { get; set; }

        // Properties relevant only to Client-managed Secret Mode are omitted
        // like signingResponse, decryptionResponse
    }
}