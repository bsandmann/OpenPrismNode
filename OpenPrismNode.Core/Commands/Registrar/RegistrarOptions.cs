namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents options for DID registration operations.
    /// </summary>
    public class RegistrarOptions
    {
        /// <summary>
        /// DID method-specific options (e.g., "network": "mainnet").
        /// Using a dictionary for flexibility.
        /// </summary>
        [JsonExtensionData] // Catches any other properties
        public Dictionary<string, object>? MethodSpecificOptions { get; set; }

        // --- Internal Secret Mode Options ---

        /// <summary>
        /// If true, the registrar stores generated/provided secrets internally. Defaults to true.
        /// </summary>
        [JsonPropertyName("storeSecrets")]
        public bool? StoreSecrets { get; set; } = true; // Default per requirement

        /// <summary>
        /// If true, the registrar returns generated secrets to the client. Defaults to true.
        /// </summary>
        [JsonPropertyName("returnSecrets")]
        public bool? ReturnSecrets { get; set; } = true; // Default per requirement

        // --- Client-managed Secret Mode Option (Included for completeness, but controller checks prevent usage) ---
        /// <summary>
        /// If true, enables client-managed secret mode (Not supported by this implementation).
        /// </summary>
        [JsonPropertyName("clientSecretMode")]
        public bool? ClientSecretMode { get; set; }

        // --- Extensions ---
        /// <summary>
        /// Optional: Request additional verification methods to be generated/added.
        /// </summary>
        [JsonPropertyName("requestVerificationMethod")]
        public List<RegistrarVerificationMethodTemplate>? RequestVerificationMethod { get; set; }

        // Add other universal options from the spec if needed
    }
}