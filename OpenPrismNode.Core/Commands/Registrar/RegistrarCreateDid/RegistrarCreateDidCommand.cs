namespace OpenPrismNode.Core.Commands.Registrar.RegistrarCreateDid
{
    using MediatR;

    /// <summary>
    /// MediatR command to handle the creation of a DID via the registrar.
    /// </summary>
    public class RegistrarCreateDidCommand : IRequest<RegistrarResponseDto>
    {
        public string? Method { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public RegistrarDidDocument? DidDocument { get; }
        public string? Did { get; } // Optional input DID

        /// <summary>
        /// Creates a new instance of the RegistrarCreateDidCommand.
        /// </summary>
        /// <param name="method">The DID method (required if did is null).</param>
        /// <param name="options">Registration options (defaults applied by controller).</param>
        /// <param name="secret">Secret material (e.g., private keys for Internal Mode).</param>
        /// <param name="didDocument">The initial DID document.</param>
        /// <param name="did">Optional pre-defined DID string.</param>
        public RegistrarCreateDidCommand(
            string? method,
            RegistrarOptions options,
            RegistrarSecret? secret,
            RegistrarDidDocument? didDocument,
            string? did)
        {
            // Add validation here or expect controller to handle basics
            Method = method;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Secret = secret;
            DidDocument = didDocument;
            Did = did;

             // Example basic validation (Controller should do more specific checks)
             if (string.IsNullOrEmpty(Method) && string.IsNullOrEmpty(Did))
             {
                 throw new ArgumentException("Either Method or Did must be provided for creation.");
             }
             if (!string.IsNullOrEmpty(Method) && !string.IsNullOrEmpty(Did))
             {
                 throw new ArgumentException("Method and Did cannot both be provided for creation.");
             }
        }
    }
}