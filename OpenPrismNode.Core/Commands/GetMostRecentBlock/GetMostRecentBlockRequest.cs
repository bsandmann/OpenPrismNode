namespace OpenPrismNode.Core.Commands.GetMostRecentBlock;

using FluentResults;
using MediatR;
using Models;
using OpenPrismNode.Core.Entities;

public class GetMostRecentBlockRequest : IRequest<Result<BlockEntity>>
{
    public GetMostRecentBlockRequest(LedgerType networkType)
    {
        NetworkType = networkType;
    }

    public LedgerType NetworkType { get; }
}