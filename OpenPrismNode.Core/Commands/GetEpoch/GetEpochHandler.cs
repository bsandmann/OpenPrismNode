namespace OpenPrismNode.Core.Commands.GetEpoch;

using Common;
using Entities;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler just to verify if a epoch already exists in the db
/// </summary>
public class GetEpochHandler : IRequestHandler<GetEpochRequest, Result<EpochEntity>>
{
    private readonly DataContext _context;
    private readonly IAppCache _cache;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public GetEpochHandler(DataContext context, IAppCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<EpochEntity>> Handle(GetEpochRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.EpochEntity_by_Id}{request.Ledger}_{request.EpochNumber}";
        var isCached = _cache.TryGetValue(cacheKey, out EpochEntity cachedEpochEntity);
        if (isCached)
        {
            return Result.Ok(cachedEpochEntity);
        }

        var epochEntity = await _context.EpochEntities.FirstOrDefaultAsync(
            p => p.EpochNumber == request.EpochNumber && p.Ledger == request.Ledger, 
            cancellationToken);

        if (epochEntity is null)
        {
            return Result.Fail($"Epoch {request.EpochNumber} could not be found for ledger {request.Ledger}");
        }

        _cache.Add(cacheKey, epochEntity);
        return Result.Ok(epochEntity);
    }
}