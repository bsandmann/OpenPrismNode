namespace OpenPrismNode.Core.Commands.DeleteTransaction;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// This handler is able to delete single transactions, but only when deleted in the correct order of dependency.
/// Delete a createDid Operation is for example not possible, when other operations like updateDid 
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