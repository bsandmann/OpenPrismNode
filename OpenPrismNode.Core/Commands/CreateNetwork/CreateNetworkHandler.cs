namespace OpenPrismNode.Core.Commands.CreateNetwork;

using Entities;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;

public class CreateNetworkHandler : IRequestHandler<CreateNetworkRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateNetworkHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var existingNetwork = await _context.PrismNetworkEntities.FirstOrDefaultAsync(p => p.NetworkType == request.LedgerType, cancellationToken: cancellationToken);
        var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Unspecified);
        if (existingNetwork is null)
        {
            var network = new NetworkEntity()
            {
                NetworkType = request.LedgerType,
                LastSynced = dateTimeNow
            };

            await _context.AddAsync(network, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}