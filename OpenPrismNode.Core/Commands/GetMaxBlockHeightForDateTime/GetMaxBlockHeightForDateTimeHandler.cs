namespace OpenPrismNode.Core.Commands.GetMaxBlockHeightForDateTime;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetMaxBlockHeightForDateTimeHandler : IRequestHandler<GetMaxBlockHeightForDateTimeRequest, Result<int>>
{
    private readonly DataContext _context;

    public GetMaxBlockHeightForDateTimeHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(GetMaxBlockHeightForDateTimeRequest request, CancellationToken cancellationToken)
    {
        var versionTime = DateTime.SpecifyKind(request.VersionTime, DateTimeKind.Unspecified);
        var maxBlockHeight = await _context.BlockEntities
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