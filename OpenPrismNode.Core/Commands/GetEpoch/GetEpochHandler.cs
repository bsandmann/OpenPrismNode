namespace OpenPrismNode.Core.Commands.GetEpoch;

using Entities;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler just to verify if a epoch already exists in the db
/// </summary>
public class GetEpochHandler : IRequestHandler<GetEpochRequest, Result<EpochEntity>>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public GetEpochHandler(DataContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<Result<EpochEntity>> Handle(GetEpochRequest request, CancellationToken cancellationToken)
    {
        // TODO can heaviliy rely on caching
        
        var epochEntity = await _context.EpochEntities.FirstOrDefaultAsync(p => p.EpochNumber == request.EpochNumber && p.NetworkType == request.NetworkType, cancellationToken);
        if (epochEntity is null)
        {
            return Result.Fail($"Epoch {request.EpochNumber} could not be found");
        }

        return Result.Ok(epochEntity);
    }
}