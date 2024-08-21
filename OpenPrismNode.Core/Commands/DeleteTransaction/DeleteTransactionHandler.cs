namespace OpenPrismNode.Core.Commands.DeleteTransaction;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// This handler is able to delete single transactions, but only when deleted in the correct order of dependency.
/// Delete a createDid Operation is for example not possible, when other operations like updateDid or issueCredential
/// rely on them. Not being able to delete any operations is by design and not a shortcomming of the handler
/// </summary>
public class DeleteTransactionHandler : IRequestHandler<DeleteTransactionRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public DeleteTransactionHandler(DataContext context)
    {
        this._context = context;
    }

    public async Task<Result> Handle(DeleteTransactionRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var transaction = await _context.TransactionEntities.FirstOrDefaultAsync(p => p.TransactionHash == request.TransactionHash.Value && p.BlockHeight == request.BlockHeight && p.BlockHashPrefix == request.BlockHashPrefix!.Value, cancellationToken: cancellationToken);
            if (transaction is not null)
            {
                // we have a limitation in the SQL db here: cascade deletes are only allowed for one entity
                // (in our case the createDid-Entity) which is connnected to the PrismPublicKey-Table.
                // But the PrismPublicKey-Table is shared with the UpdateDid-Entity. So when deleting
                // a transaction we must first remove all connected updateDid-Public_keys 
                // var connectedPublicKeyEntitiesForUpdateDid = await _context.PrismPublicKeyEntities.Where(p => p.PrismUpdateDidEntity!.PrismTransactionEntity.TransactionHash == request.TransactionHash.Value).ToListAsync(cancellationToken: cancellationToken);
                // _context.RemoveRange(connectedPublicKeyEntitiesForUpdateDid);
                // var connectedPublicKeyRemoveEntities = await _context.PrismPublicKeyRemoveEntities.Where(p=>p.PrismUpdateDidEntity.PrismTransactionEntity.TransactionHash == request.TransactionHash.Value).ToListAsync(cancellationToken: cancellationToken);
                // _context.RemoveRange(connectedPublicKeyRemoveEntities);
                // var connectedPrismServiceEntities = await _context.PrismServiceEntities.Where(p=>p.PrismUpdateDidEntity.PrismTransactionEntity.TransactionHash == request.TransactionHash.Value).ToListAsync(cancellationToken: cancellationToken);
                // _context.RemoveRange(connectedPrismServiceEntities);
                _context.Remove(transaction); 
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }

            return Result.Fail($"Transaction {request.TransactionHash.Value} not found");
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting transaction", e);
        }
    }
}