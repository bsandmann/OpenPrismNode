namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using FluentResults;
using MediatR;
using PostgresModels;

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