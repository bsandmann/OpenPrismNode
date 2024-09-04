namespace OpenPrismNode.Core.Commands.DeleteEmptyEpoch;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class DeleteEmptyEpochRequest : IRequest<Result>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public DeleteEmptyEpochRequest(int epoch, LedgerType ledger)
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