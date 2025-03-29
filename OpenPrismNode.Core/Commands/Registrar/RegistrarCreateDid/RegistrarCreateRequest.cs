namespace OpenPrismNode.Core.Commands.Registrar.RegistrarCreateDid
{
    using FluentResults;
    using MediatR;

    /// <summary>
    /// MediatR request to create a DID using the Registrar.
    /// </summary>
    public class RegistrarCreateDidRequest : IRequest<Result<RegistrarResponseDto>>
    {
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public RegistrarDidDocument DidDocument { get; }

        public RegistrarCreateDidRequest(
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