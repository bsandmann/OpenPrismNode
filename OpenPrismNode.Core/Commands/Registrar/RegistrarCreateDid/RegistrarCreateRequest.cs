namespace OpenPrismNode.Core.Commands.Registrar.RegistrarCreateDid
{
    using FluentResults;
    using MediatR;

    /// <summary>
    /// MediatR request to create a DID using the Registrar.
    /// </summary>
    public class RegistrarCreateDidRequest : IRequest<Result<RegistrarResponseDto>>
    {
        public string Method { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public RegistrarDidDocument DidDocument { get; }
        public string? Did { get; } // Optional DID input

        public RegistrarCreateDidRequest(
            string method,
            RegistrarOptions options,
            RegistrarSecret? secret,
            RegistrarDidDocument didDocument,
            string? did)
        {
            Method = method;
            Options = options; // Assume defaults are handled/enforced by controller or handler
            Secret = secret;
            DidDocument = didDocument;
            Did = did;
        }
    }

}