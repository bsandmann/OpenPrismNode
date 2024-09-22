namespace OpenPrismNode.Core.Commands.DeleteTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class DeleteTransactionRequest : IRequest<Result>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="transactionHash"></param>
    /// <param name="blockHeight"></param>
    /// <param name="blockHashPrefix"></param>
    public DeleteTransactionRequest(Hash transactionHash, int blockHeight, int? blockHashPrefix)
    {
        TransactionHash = transactionHash;
        BlockHeight = blockHeight;
        BlockHashPrefix = blockHashPrefix;
    }
    
    /// <summary>
    /// TransactionHash of the operation
    /// </summary>
    public Hash TransactionHash { get; } 
    public int BlockHeight { get; }
    public int? BlockHashPrefix { get; }
}