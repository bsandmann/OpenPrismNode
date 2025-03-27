namespace OpenPrismNode.Core.Commands.Registrar.RegistrarDeactivateDid
{
    using FluentResults;
    using MediatR;

    /// <summary>
    /// MediatR request to deactivate a DID using the Registrar.
    /// </summary>
    public class RegistrarDeactivateDidRequest : IRequest<Result<RegistrarResponseDto>>
    {
        public string Did { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }

        public RegistrarDeactivateDidRequest(
            string did,
            RegistrarOptions options,
            RegistrarSecret? secret)
        {
            Did = did;
            Options = options;
            Secret = secret;
        }
    }
}