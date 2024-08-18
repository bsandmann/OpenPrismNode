namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public record ProcessTransactionRequest : IRequest<Result>
{
    public ProcessTransactionRequest(Block block, Transaction transaction)
    {
        Block = block;
        Transaction = transaction;
    }
    
    public Block Block { get; }
    public Transaction Transaction { get; }
}