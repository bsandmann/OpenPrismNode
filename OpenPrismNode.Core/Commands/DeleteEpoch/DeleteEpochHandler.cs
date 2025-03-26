namespace OpenPrismNode.Core.Commands.DeleteEpoch;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core;

/// <summary>
/// Handler just to delete a empty epoch in the db
/// </summary>
public class DeleteEpochHandler : IRequestHandler<DeleteEpochRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    public DeleteEpochHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteEpochRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var emptyEpoch = await context.EpochEntities.FirstOrDefaultAsync(p => p.EpochNumber == request.Epoch, cancellationToken);
            if (emptyEpoch is null)
            {
                return Result.Fail("Epoch could not be found or is not empty");
            }

            context.EpochEntities.Remove(emptyEpoch);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting empty epoch", e);
        }

        return Result.Ok();
    }
}