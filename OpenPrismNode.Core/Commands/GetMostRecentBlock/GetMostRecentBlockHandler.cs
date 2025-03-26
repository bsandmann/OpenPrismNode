namespace OpenPrismNode.Core.Commands.GetMostRecentBlock;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Entities;

public class GetMostRecentBlockHandler : IRequestHandler<GetMostRecentBlockRequest, Result<BlockEntity>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetMostRecentBlockHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<BlockEntity>> Handle(GetMostRecentBlockRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var mostRecentBlock = await context.BlockEntities
            .Include(p => p.EpochEntity)
            .Where(b => b.IsFork == false && b.EpochEntity.Ledger == request.Ledger)
            .OrderByDescending(b => b.BlockHeight)
            .FirstOrDefaultAsync(cancellationToken);

        if (mostRecentBlock == null)
        {
            return Result.Fail("No blocks found in the database.");
        }

        mostRecentBlock.TimeUtc = DateTime.SpecifyKind(mostRecentBlock.TimeUtc, DateTimeKind.Utc);
        mostRecentBlock.LastParsedOnUtc = mostRecentBlock.LastParsedOnUtc is not null ? DateTime.SpecifyKind(mostRecentBlock.LastParsedOnUtc!.Value, DateTimeKind.Utc) : null;
        return Result.Ok(mostRecentBlock);
    }
}