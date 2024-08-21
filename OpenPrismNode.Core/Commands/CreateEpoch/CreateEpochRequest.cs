namespace OpenPrismNode.Core.Commands.CreateEpoch;

using Entities;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class CreateEpochRequest : IRequest<Result<EpochEntity>>
{
    public CreateEpochRequest(LedgerType ledger, int epochNumber)
    {
        Ledger = ledger;
        EpochNumber = epochNumber;
    }

    public LedgerType Ledger { get; }
    public int EpochNumber { get; }
}