namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForCreateDid
{
    using FluentResults;
    using MediatR;

    /// <summary>
    /// MediatR request to create a DID using the Registrar.
    /// </summary>
    public class CreateSignedAtalaOperationForCreateDidRequest : IRequest<Result<CreateSignedAtalaOperationForCreateDidResponse>>
    {
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public RegistrarDidDocument DidDocument { get; }

        public CreateSignedAtalaOperationForCreateDidRequest(
            RegistrarOptions options,
            RegistrarSecret? secret,
            RegistrarDidDocument didDocument
        )
        {
            Options = options;
            Secret = secret;
            DidDocument = didDocument;
        }
    }
}