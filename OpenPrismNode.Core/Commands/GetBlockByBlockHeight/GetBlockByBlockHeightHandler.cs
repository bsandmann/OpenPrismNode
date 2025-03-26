namespace OpenPrismNode.Core.Commands.GetBlockByBlockHeight;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Entities;

public class GetBlockByBlockHeightHandler : IRequestHandler<GetBlockByBlockHeightRequest, Result<BlockEntity>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetBlockByBlockHeightHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<BlockEntity>> Handle(GetBlockByBlockHeightRequest request, CancellationToken cancellationToken)
    {

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var block = await context.BlockEntities
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