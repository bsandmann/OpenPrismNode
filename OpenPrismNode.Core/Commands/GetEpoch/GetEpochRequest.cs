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
    public GetEpochRequest(LedgerType networkType, int epochNumber)
    {
        EpochNumber = epochNumber;
        NetworkType = networkType;
    }
    
    public int EpochNumber { get; }
    
    public LedgerType NetworkType { get; }
}