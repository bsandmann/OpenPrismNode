namespace OpenPrismNode.Core.Commands.GetMaxBlockHeightForDateTime;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class GetMaxBlockHeightForDateTimeRequest : IRequest<Result<int>>
{
    public GetMaxBlockHeightForDateTimeRequest(LedgerType ledger, DateTime versionTime)
    {
        Ledger = ledger;
        VersionTime = versionTime;
    }

    public LedgerType Ledger { get; }
    public DateTime VersionTime { get; }
}