namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForDeactivateDid;

using FluentResults;
using MediatR;
using Models;

public class CreateSignedAtalaOperationForDeactivateDidRequest : IRequest<Result<CreateSignedAtalaOperationForDeactivateDidResponse>>
{
    public RegistrarOptions Options { get; }

    public LedgerType LedgerType { get; }
    public string Did { get; }

    public CreateSignedAtalaOperationForDeactivateDidRequest(
        RegistrarOptions options,
        LedgerType ledgerType,
        string did
    )
    {
        Options = options;
        LedgerType = ledgerType;
        Did = did;
    }
}