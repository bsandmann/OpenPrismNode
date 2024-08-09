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
            .Where(b => b.IsFork == false && b.EpochEntity.NetworkType == request.NetworkType)
            .OrderByDescending(b => b.BlockHeight)
            .FirstOrDefaultAsync(cancellationToken);

        if (mostRecentBlock == null)
        {
            return Result.Fail("No blocks found in the database.");
        }

        return Result.Ok(mostRecentBlock);
    }
}