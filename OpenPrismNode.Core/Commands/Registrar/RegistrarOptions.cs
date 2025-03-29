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

        /// <summary>
        /// The wallet ID for publishing the DID on chain. The wallet must be funded.
        /// This is required
        /// </summary>
        [JsonPropertyName("walletId")]
        public string WalletId { get; set; }

        /// <summary>
        /// The optional network identitifier (e.g. mainnet or preprod)
        /// If the network is specified but not matching the settings of the OPN this will return an error
        /// </summary>
        public string? Network { get; set; }



    }
}