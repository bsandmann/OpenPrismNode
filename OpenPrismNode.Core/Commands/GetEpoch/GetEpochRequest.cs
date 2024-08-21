namespace OpenPrismNode.Core.Commands.GetEpoch;

using FluentResults;
using MediatR;
using Models;
using OpenPrismNode.Core.Entities;

/// <summary>
/// Request
/// </summary>
public class GetEpochRequest : IRequest<Result<EpochEntity>>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public GetEpochRequest(LedgerType ledger, int epochNumber)
    {
        EpochNumber = epochNumber;
        Ledger = ledger;
    }
    
    public int EpochNumber { get; }
    
    public LedgerType Ledger { get; }
}