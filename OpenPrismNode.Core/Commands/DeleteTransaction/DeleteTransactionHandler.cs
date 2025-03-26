namespace OpenPrismNode.Core.Commands.DeleteTransaction;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// This handler is able to delete single transactions, but only when deleted in the correct order of dependency.
/// Delete a createDid Operation is for example not possible, when other operations like updateDid 
/// rely on them. Not being able to delete any operations is by design and not a shortcomming of the handler
/// </summary>
public class DeleteTransactionHandler : IRequestHandler<DeleteTransactionRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    public DeleteTransactionHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result> Handle(DeleteTransactionRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var transaction = await context.TransactionEntities.FirstOrDefaultAsync(p => p.TransactionHash == request.TransactionHash.Value && p.BlockHeight == request.BlockHeight && p.BlockHashPrefix == request.BlockHashPrefix!.Value, cancellationToken: cancellationToken);
            if (transaction is not null)
            {
                context.Remove(transaction);
                await context.SaveChangesAsync(cancellationToken);
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