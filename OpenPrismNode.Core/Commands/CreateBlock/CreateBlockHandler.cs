namespace OpenPrismNode.Core.Commands.CreateBlock;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

/// <summary>
/// Handler to create new blocks inside the node-database to represent a block
/// </summary>
public class CreateBlockHandler : IRequestHandler<CreateBlockRequest, Result<BlockEntity>>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateBlockHandler(DataContext context)
    {
        this._context = context;
    }

    /// <summary>
    /// Handler
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<BlockEntity>> Handle(CreateBlockRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var now = DateTime.UtcNow;

        if (!_context.BlockEntities.Any(p => p.BlockHeight == request.BlockHeight))
        {
            var blockEntity = new BlockEntity()
            {
                BlockHeight = request.BlockHeight,
                BlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value) ?? 0,
                BlockHash = request.BlockHash.Value!,
                EpochNumber = request.EpochNumber,
                TimeUtc = request.TimeUtc,
                TxCount = request.TxCount,
                LastParsedOnUtc = now,
                PreviousBlockHeight = request.PreviousBlockHeight,
                PreviousBlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.PreviousBlockHash?.Value),
                IsFork = request.IsFork
            };

            await _context.AddAsync(blockEntity, cancellationToken);

            // Update network LastSynced time
            await _context.PrismNetworkEntities
                .Where(n => n.NetworkType == request.NetworkType)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.LastSynced, now), cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok(blockEntity);
        }
        else
        {
            // if the blockheigth already exists we have to determine if the block is a fork or just the same block
            // which was already parsed and readded by mistake.
            throw new NotImplementedException();
        }
    }
}