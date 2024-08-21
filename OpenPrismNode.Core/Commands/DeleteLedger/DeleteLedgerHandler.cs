namespace OpenPrismNode.Core.Commands.DeleteLedger;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;

public class DeleteLedgerHandler : IRequestHandler<DeleteLedgerRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public DeleteLedgerHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteLedgerRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var existingLedger = await _context.LedgerEntities.FirstOrDefaultAsync(p => p.Ledger == request.LedgerType, cancellationToken: cancellationToken);
            if (existingLedger is null)
            {
                return Result.Fail("The ledger does not exist");
            }

            _context.Remove(existingLedger);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting ledger", ex);
        }
    }
}