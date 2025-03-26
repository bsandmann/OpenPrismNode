namespace OpenPrismNode.Core.Commands.GetBlockByBlockHash;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Entities;

public class GetBlockByBlockHashHandler : IRequestHandler<GetBlockByBlockHashRequest, Result<BlockEntity>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetBlockByBlockHashHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<BlockEntity>> Handle(GetBlockByBlockHashRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var block = await context.BlockEntities
            .Where(b => b.BlockHeight == request.BlockHeight
                        && b.BlockHashPrefix == request.BlockHashPrefix
                        && b.EpochEntity.Ledger == request.Ledger)
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
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (block == null)
        {
            return Result.Fail($"No block found with height {request.BlockHeight} and hash prefix {request.BlockHashPrefix} in the {request.Ledger} network.");
        }

        block.TimeUtc = DateTime.SpecifyKind(block.TimeUtc, DateTimeKind.Utc);
        block.LastParsedOnUtc = block.LastParsedOnUtc is not null ? DateTime.SpecifyKind(block.LastParsedOnUtc!.Value, DateTimeKind.Utc) : null;
        return Result.Ok(block);
    }
}