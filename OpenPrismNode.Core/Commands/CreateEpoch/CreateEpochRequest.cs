namespace OpenPrismNode.Core.Commands.CreateEpoch;

using Entities;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class CreateEpochRequest : IRequest<Result<EpochEntity>>
{
    public CreateEpochRequest(LedgerType networkType, int epochNumber)
    {
        NetworkType = networkType;
        EpochNumber = epochNumber;
    }

    public LedgerType NetworkType { get; }
    public int EpochNumber { get; }
}