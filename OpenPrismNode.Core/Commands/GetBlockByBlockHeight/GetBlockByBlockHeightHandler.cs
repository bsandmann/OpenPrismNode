namespace OpenPrismNode.Core.Commands.GetBlockByBlockHeight;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

public class GetBlockByBlockHeightHandler : IRequestHandler<GetBlockByBlockHeightRequest, Result<BlockEntity>>
{
    private readonly DataContext _context;

    public GetBlockByBlockHeightHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<BlockEntity>> Handle(GetBlockByBlockHeightRequest request, CancellationToken cancellationToken)
    {
        var block = await _context.BlockEntities
            .Where(b => b.IsFork == false && b.EpochEntity.Ledger == request.Ledger)
            .FirstOrDefaultAsync(p => p.BlockHeight == request.BlockHeight, cancellationToken);

        if (block == null)
        {
            return Result.Fail($"No blocks found in the database with the given block height: {request.BlockHeight}");
        }
        
        block.TimeUtc = DateTime.SpecifyKind(block.TimeUtc, DateTimeKind.Utc);
        block.LastParsedOnUtc = block.LastParsedOnUtc is not null ? DateTime.SpecifyKind(block.LastParsedOnUtc.Value, DateTimeKind.Utc) : null;
        return Result.Ok(block);
    }
}