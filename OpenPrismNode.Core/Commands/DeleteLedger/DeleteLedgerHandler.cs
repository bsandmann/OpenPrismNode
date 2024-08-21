namespace OpenPrismNode.Core.Commands.DeleteLedger;

using DeleteEpoch;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;

public class DeleteLedgerHandler : IRequestHandler<DeleteLedgerRequest, Result>
{
    private readonly DataContext _context;
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public DeleteLedgerHandler(DataContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<Result> Handle(DeleteLedgerRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var epochs = await _context.EpochEntities.Select(p => new { p.Ledger, p.EpochNumber }).Where(p => p.Ledger == request.LedgerType).ToListAsync(cancellationToken: cancellationToken);
            if (!epochs.Any())
            {
                return Result.Fail("No epochs found for the ledger");
            }

            foreach (var epoch in epochs.OrderByDescending(p=>p.EpochNumber))
            {
                var epochDeleteResult = await _mediator.Send(new DeleteEpochRequest(epoch.EpochNumber, request.LedgerType), cancellationToken);
                if (epochDeleteResult.IsFailed)
                {
                    return epochDeleteResult;
                }
            }

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