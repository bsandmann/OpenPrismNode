namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForUpdateDid
{
    using FluentResults;
    using MediatR;
    using Models;

    /// <summary>
    /// MediatR request to update a DID using the Registrar.
    /// </summary>
    public class CreateSignedAtalaOperationForUpdateDidRequest : IRequest<Result<CreateSignedAtalaOperationForUpdateDidResponse>>
    {
        public RegistrarOptions Options { get; }
        public RegistrarSecret? Secret { get; }
        public List<RegistrarDidDocument> DidDocuments { get; }
        public List<string> Operations { get; }

        public LedgerType LedgerType { get; }

        public string Did { get; }

        public CreateSignedAtalaOperationForUpdateDidRequest(
            RegistrarOptions options,
            RegistrarSecret? secret,
            List<RegistrarDidDocument> didDocuments,
            List<string> operations,
            LedgerType ledgerType,
            string did
        )
        {
            Options = options;
            Secret = secret;
            DidDocuments = didDocuments;
            Operations = operations;
            LedgerType = ledgerType;
            Did = did;
        }
    }
}