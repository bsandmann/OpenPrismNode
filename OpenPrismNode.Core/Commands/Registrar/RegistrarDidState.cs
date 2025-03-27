namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the state of the DID registration process. Part of the response.
    /// </summary>
    public class RegistrarDidState
    {
        public const string FinishedState = "finished";
        public const string FailedState = "failed";
        public const string ActionState = "action";
        public const string WaitState = "wait";

        /// <summary>
        /// The current state: "finished", "failed", "action", "wait". Required.
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = null!;

        /// <summary>
        /// The DID that is the subject of the operation. Required if state is "finished".
        /// Matches input DID for update/deactivate.
        /// </summary>
        [JsonPropertyName("did")]
        public string? Did { get; set; } // Nullable because it might not exist yet or on failure

        /// <summary>
        /// Secrets returned to the client (only if options.returnSecrets=true).
        /// </summary>
        [JsonPropertyName("secret")]
        public RegistrarSecret? Secret { get; set; } // Nullable, only present when requested

        /// <summary>
        /// The resulting DID document after a successful operation. Optional.
        /// </summary>
        [JsonPropertyName("didDocument")]
        public RegistrarDidDocument? DidDocument { get; set; } // Nullable

        // --- Fields for specific states ---

        /// <summary>
        /// Reason for failure (present if state="failed").
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// Type of action required by the client (present if state="action").
        /// E.g., "fundingRequired", "redirect".
        /// </summary>
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        /// <summary>
        /// Description for the action or wait state.
        /// </summary>
        [JsonPropertyName("description")] // Added for action state example
        public string? Description { get; set; }

        /// <summary>
        /// URL for redirection (present if state="action" and action="redirect").
        /// </summary>
        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// Explanation for waiting (present if state="wait").
        /// </summary>
        [JsonPropertyName("wait")]
        public string? Wait { get; set; }

        /// <summary>
        /// Estimated wait time in milliseconds (optional, if state="wait").
        /// </summary>
        [JsonPropertyName("waitTime")]
        public long? WaitTime { get; set; }

        // Fields relevant only to Client-managed Secret Mode are omitted
        // like verificationMethodTemplate, signingRequest, decryptionRequest
    }

}