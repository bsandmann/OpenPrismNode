namespace OpenPrismNode.Core.Commands.DeleteLedger;

using DeleteEpoch;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core;

public class DeleteLedgerHandler : IRequestHandler<DeleteLedgerRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="mediator"></param>
    public DeleteLedgerHandler(IServiceScopeFactory serviceScopeFactory, IMediator mediator)
    {
         _serviceScopeFactory = serviceScopeFactory;
        _mediator = mediator;
    }

    public async Task<Result> Handle(DeleteLedgerRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var epochs = await context.EpochEntities.Select(p => new { p.Ledger, p.EpochNumber }).Where(p => p.Ledger == request.LedgerType).ToListAsync(cancellationToken: cancellationToken);
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

            var existingLedger = await context.LedgerEntities.FirstOrDefaultAsync(p => p.Ledger == request.LedgerType, cancellationToken: cancellationToken);
            if (existingLedger is null)
            {
                return Result.Fail("The ledger does not exist");
            }

            context.Remove(existingLedger);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting ledger", ex);
        }
    }
}