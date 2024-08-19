namespace OpenPrismNode.Core.Commands.DeleteLedger;

using CreateNetwork;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

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
            var existingNetwork = await _context.PrismNetworkEntities.FirstOrDefaultAsync(p => p.NetworkType == request.LedgerType, cancellationToken: cancellationToken);
            if (existingNetwork is null)
            {
                return Result.Fail("The network does not exist");
            }

            _context.Remove(existingNetwork);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}