﻿namespace OpenPrismNode.Core.Commands.DeleteEpoch;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;

/// <summary>
/// Handler just to delete a empty epoch in the db
/// </summary>
public class DeleteEpochHandler : IRequestHandler<DeleteEpochRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public DeleteEpochHandler(DataContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteEpochRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var emptyEpoch = await _context.EpochEntities.FirstOrDefaultAsync(p => p.EpochNumber == request.Epoch, cancellationToken);
            if (emptyEpoch is null)
            {
                return Result.Fail("Epoch could not be found or is not empty");
            }

            _context.EpochEntities.Remove(emptyEpoch);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting empty epoch", e);
        }

        return Result.Ok();
    }
}