namespace OpenPrismNode.Core.Commands.GetBlockByBlockHash;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

public class GetBlockByBlockHashHandler : IRequestHandler<GetBlockByBlockHashRequest, Result<BlockEntity>>
{
    private readonly DataContext _context;

    public GetBlockByBlockHashHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<BlockEntity>> Handle(GetBlockByBlockHashRequest request, CancellationToken cancellationToken)
    {
        var block = await _context.BlockEntities
            .Where(b => b.BlockHeight == request.BlockHeight
                        && b.BlockHashPrefix == request.BlockHashPrefix
                        && b.EpochEntity.NetworkType == request.NetworkType)
            .Select(b => new BlockEntity
            {
                BlockHeight = b.BlockHeight,
                BlockHashPrefix = b.BlockHashPrefix,
                BlockHash = b.BlockHash,
                TimeUtc = b.TimeUtc,
                TxCount = b.TxCount,
                LastParsedOnUtc = b.LastParsedOnUtc,
                EpochNumber = b.EpochNumber,
                IsFork = b.IsFork,
                PreviousBlockHeight = b.PreviousBlockHeight,
                PreviousBlockHashPrefix = b.PreviousBlockHashPrefix
                // Add other properties as needed
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (block == null)
        {
            return Result.Fail($"No block found with height {request.BlockHeight} and hash prefix {request.BlockHashPrefix} in the {request.NetworkType} network.");
        }

        block.TimeUtc = DateTime.SpecifyKind(block.TimeUtc, DateTimeKind.Utc);
        block.LastParsedOnUtc = block.LastParsedOnUtc is not null ? DateTime.SpecifyKind(block.LastParsedOnUtc!.Value, DateTimeKind.Utc) : null;
        return Result.Ok(block);
    }
}