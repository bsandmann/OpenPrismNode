namespace OpenPrismNode.Core.Commands.GetNextOperation;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class GetNexOperationHandler : IRequestHandler<GetNextOperationRequest, Result<DateTime?>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetNexOperationHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<DateTime?>> Handle(GetNextOperationRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var nextOperationUpdate = await context.UpdateDidEntities
            .Select(p => new { p.BlockHeight, p.PreviousOperationHash })
            .FirstOrDefaultAsync(p => p.PreviousOperationHash == request.CurrentOperationHash, cancellationToken);
        var nextOperationDeactivate = await context.DeactivateDidEntities
            .Select(p => new { p.BlockHeight, p.PreviousOperationHash })
            .FirstOrDefaultAsync(p => p.PreviousOperationHash == request.CurrentOperationHash, cancellationToken);

        if (nextOperationUpdate is null && nextOperationDeactivate is null)
        {
            // No next operation found
            return Result.Ok<DateTime?>(null);
        }
        else if (nextOperationUpdate is null && nextOperationDeactivate is not null)
        {
            return await GetBlockTime(nextOperationDeactivate.BlockHeight, context);
        }
        else if (nextOperationUpdate is not null && nextOperationDeactivate is null)
        {
            return await GetBlockTime(nextOperationUpdate.BlockHeight, context);
        }

        if (nextOperationUpdate.BlockHeight >= nextOperationDeactivate.BlockHeight)
        {
            return await GetBlockTime(nextOperationUpdate.BlockHeight, context);
        }

        return await GetBlockTime(nextOperationDeactivate.BlockHeight, context);
    }

    private async Task<Result<DateTime?>> GetBlockTime(int blockHeight, DataContext context)
    {
        var blockTime = await context.BlockEntities
            .Where(b => b.BlockHeight == blockHeight)
            .Select(b => b.TimeUtc)
            .FirstOrDefaultAsync();

        return Result.Ok<DateTime?>(blockTime);
    }
}