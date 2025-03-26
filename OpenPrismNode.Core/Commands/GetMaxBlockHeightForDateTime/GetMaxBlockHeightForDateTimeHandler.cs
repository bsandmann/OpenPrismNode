namespace OpenPrismNode.Core.Commands.GetMaxBlockHeightForDateTime;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class GetMaxBlockHeightForDateTimeHandler : IRequestHandler<GetMaxBlockHeightForDateTimeRequest, Result<int>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetMaxBlockHeightForDateTimeHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<int>> Handle(GetMaxBlockHeightForDateTimeRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var versionTime = DateTime.SpecifyKind(request.VersionTime, DateTimeKind.Unspecified);
        var maxBlockHeight = await context.BlockEntities
            .Where(b => b.IsFork == false &&
                        b.EpochEntity.Ledger == request.Ledger &&
                        b.TimeUtc <= versionTime && b.TimeUtc > DateTime.MinValue)
            .OrderByDescending(b => b.BlockHeight)
            .Select(b => new { b.BlockHeight, b.TimeUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (maxBlockHeight is null || maxBlockHeight.BlockHeight == 0)
        {
            return Result.Fail($"No blocks found in the database within the given constraint TimeUtc <= {versionTime}");
        }

        return Result.Ok(maxBlockHeight.BlockHeight);
    }
}