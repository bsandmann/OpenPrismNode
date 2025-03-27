namespace OpenPrismNode.Core.Commands.Registrar.RegistrarUpdateDid
{
    using System.Collections.Generic;
    using FluentResults;
    using MediatR;

    /// <summary>
    /// MediatR request to update a DID using the Registrar.
    /// </summary>
    public class RegistrarUpdateDidRequest : IRequest<Result<RegistrarResponseDto>>
    {
        public string Did { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public List<string> DidDocumentOperation { get; }
        public List<RegistrarDidDocument>? DidDocument { get; } // Array for update

        public RegistrarUpdateDidRequest(
            string did,
            RegistrarOptions options,
            RegistrarSecret? secret,
            List<string> didDocumentOperation,
            List<RegistrarDidDocument>? didDocument)
        {
            Did = did;
            Options = options;
            Secret = secret;
            DidDocumentOperation = didDocumentOperation;
            DidDocument = didDocument;
        }
    }
}