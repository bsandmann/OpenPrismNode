namespace OpenPrismNode.Core.Commands.DeleteEpoch;

using FluentResults;
using MediatR;
using Models;

public class DeleteEpochRequest : IRequest<Result>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public DeleteEpochRequest(int epoch, LedgerType ledger)
    {
        Epoch = epoch;
        Ledger = ledger;
    }
    
    /// <summary>
    /// Epoch to be deleted
    /// </summary>
    public int Epoch { get; } 
    
    /// <summary>
    /// Ledger (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType Ledger { get; }
}