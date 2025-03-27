namespace OpenPrismNode.Core.Commands.Registrar.RegistrarDeactivateDid
{
    using MediatR;

    /// <summary>
    /// MediatR command to handle the deactivation of a DID via the registrar.
    /// </summary>
    public class RegistrarDeactivateDidCommand : IRequest<RegistrarResponseDto>
    {
        public string Did { get; }
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }

        /// <summary>
        /// Creates a new instance of the RegistrarDeactivateDidCommand.
        /// </summary>
        /// <param name="did">The DID to deactivate (required).</param>
        /// <param name="options">Registration options (defaults applied by controller).</param>
        /// <param name="secret">Secret material (e.g., private keys for auth in Internal Mode).</param>
        public RegistrarDeactivateDidCommand(
            string did,
            RegistrarOptions options,
            RegistrarSecret? secret)
        {
            if (string.IsNullOrEmpty(did))
            {
                throw new ArgumentException("DID cannot be null or empty for deactivation.", nameof(did));
            }
            Did = did;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Secret = secret;
        }
    }
}