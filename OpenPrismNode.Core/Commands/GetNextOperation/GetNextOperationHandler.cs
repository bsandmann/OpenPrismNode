namespace OpenPrismNode.Core.Commands.GetNextOperation;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetNexOperationHandler : IRequestHandler<GetNextOperationRequest, Result<DateTime?>>
{
    private readonly DataContext _context;

    public GetNexOperationHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<DateTime?>> Handle(GetNextOperationRequest request, CancellationToken cancellationToken)
    {
        var nextOperationUpdate = await _context.UpdateDidEntities
            .Select(p => new { p.BlockHeight, p.PreviousOperationHash })
            .FirstOrDefaultAsync(p => p.PreviousOperationHash == request.CurrentOperationHash, cancellationToken);
        var nextOperationDeactivate = await _context.DeactivateDidEntities
            .Select(p => new { p.BlockHeight, p.PreviousOperationHash })
            .FirstOrDefaultAsync(p => p.PreviousOperationHash == request.CurrentOperationHash, cancellationToken);

        if (nextOperationUpdate is null && nextOperationDeactivate is null)
        {
            // No next operation found
            return Result.Ok<DateTime?>(null);
        }
        else if (nextOperationUpdate is null && nextOperationDeactivate is not null)
        {
            return await GetBlockTime(nextOperationDeactivate.BlockHeight);
        }
        else if (nextOperationUpdate is not null && nextOperationDeactivate is null)
        {
            return await GetBlockTime(nextOperationUpdate.BlockHeight);
        }

        if (nextOperationUpdate.BlockHeight >= nextOperationDeactivate.BlockHeight)
        {
            return await GetBlockTime(nextOperationUpdate.BlockHeight);
        }

        return await GetBlockTime(nextOperationDeactivate.BlockHeight);
    }

    private async Task<Result<DateTime?>> GetBlockTime(int blockHeight)
    {
        var blockTime = await _context.BlockEntities
            .Where(b => b.BlockHeight == blockHeight)
            .Select(b => b.TimeUtc)
            .FirstOrDefaultAsync();

        return Result.Ok<DateTime?>(blockTime);
    }
}