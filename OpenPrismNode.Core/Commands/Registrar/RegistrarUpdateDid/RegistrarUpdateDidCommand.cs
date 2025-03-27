namespace OpenPrismNode.Core.Commands.Registrar.RegistrarUpdateDid
{
    using System.Collections.Generic;
    using System.Linq;
    using MediatR;

    /// <summary>
    /// MediatR command to handle the update of a DID via the registrar.
    /// </summary>
    public class RegistrarUpdateDidCommand : IRequest<RegistrarResponseDto>
    {
        public string Did { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public List<string> DidDocumentOperation { get; }
        public List<RegistrarDidDocument>? DidDocument { get; }

        /// <summary>
        /// Creates a new instance of the RegistrarUpdateDidCommand.
        /// </summary>
        /// <param name="did">The DID to update (required).</param>
        /// <param name="options">Registration options (defaults applied by controller).</param>
        /// <param name="secret">Secret material (e.g., private keys for auth in Internal Mode).</param>
        /// <param name="didDocumentOperation">The list of operations (defaults to 'setDidDocument' if empty/null).</param>
        /// <param name="didDocument">The document payload(s) corresponding to the operations.</param>
        public RegistrarUpdateDidCommand(
            string did,
            RegistrarOptions options,
            RegistrarSecret? secret,
            List<string> didDocumentOperation,
            List<RegistrarDidDocument>? didDocument)
        {
            if (string.IsNullOrEmpty(did))
            {
                throw new ArgumentException("DID cannot be null or empty for update.", nameof(did));
            }
             Did = did;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Secret = secret;
            DidDocumentOperation = didDocumentOperation?.Any() == true ? didDocumentOperation : new List<string> { RegistrarDidDocumentOperation.SetDidDocument };
            DidDocument = didDocument;

            // Add more validation if needed (e.g., ensure didDocument count matches operations if required)
        }
    }
}