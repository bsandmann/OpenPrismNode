namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using Core.DbSyncModels;
using Core.Models;
using FluentResults;
using MediatR;

public record ProcessTransactionRequest : IRequest<Result>
{
    public ProcessTransactionRequest(LedgerType ledger, Block block, Transaction transaction)
    {
        Block = block;
        Transaction = transaction;
        Ledger = ledger;
    }
    
    public Block Block { get; }
    public Transaction Transaction { get; }
    
    public LedgerType Ledger { get; }
}