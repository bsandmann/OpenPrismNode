namespace OpenPrismNode.Core.Commands.CreateEpoch;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

public class CreateEpochHandler : IRequestHandler<CreateEpochRequest, Result<EpochEntity>>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateEpochHandler(DataContext context)
    {
        this._context = context;
    }

    public async Task<Result<EpochEntity>> Handle(CreateEpochRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var existingEpoch = await _context.EpochEntities.FirstOrDefaultAsync(p => p.EpochNumber == request.EpochNumber && p.NetworkType == request.NetworkType, cancellationToken: cancellationToken);
        if (existingEpoch is null)
        {
            var epochEntity = new EpochEntity()
            {
                NetworkType = request.NetworkType,
                EpochNumber = request.EpochNumber,
            };

            await _context.AddAsync(epochEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok(epochEntity);
        }

        return Result.Ok(existingEpoch);
    }
}