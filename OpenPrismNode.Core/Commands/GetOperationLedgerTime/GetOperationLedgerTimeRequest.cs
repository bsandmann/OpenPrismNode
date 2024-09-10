namespace OpenPrismNode.Core.Commands.GetOperationLedgerTime;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class GetOperationLedgerTimeRequest : IRequest<Result<GetOperationLedgerTimeResponse>>
{
    public GetOperationLedgerTimeRequest(string versionId, LedgerType ledger)
    {
        VersionId = versionId;
        Ledger = ledger;
    }

    public string VersionId { get; set; }
    public LedgerType Ledger { get; set; }
}