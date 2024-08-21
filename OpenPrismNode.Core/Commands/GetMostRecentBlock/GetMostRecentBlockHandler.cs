namespace OpenPrismNode.Core.Commands.GetMostRecentBlock;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

public class GetMostRecentBlockHandler : IRequestHandler<GetMostRecentBlockRequest, Result<BlockEntity>>
{
    private readonly DataContext _context;

    public GetMostRecentBlockHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<BlockEntity>> Handle(GetMostRecentBlockRequest request, CancellationToken cancellationToken)
    {
        var mostRecentBlock = await _context.BlockEntities
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