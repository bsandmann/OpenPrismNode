namespace OpenPrismNode.Core.Commands.CreateEpoch;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

public class CreateEpochHandler : IRequestHandler<CreateEpochRequest, Result<EpochEntity>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateEpochHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<EpochEntity>> Handle(CreateEpochRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var existingEpoch = await context.EpochEntities.FirstOrDefaultAsync(
            p => p.EpochNumber == request.EpochNumber && p.Ledger == request.Ledger,
            cancellationToken: cancellationToken);

        if (existingEpoch is null)
        {
            var epochEntity = new EpochEntity()
            {
                Ledger = request.Ledger,
                EpochNumber = request.EpochNumber,
            };

            await context.AddAsync(epochEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Ok(epochEntity);
        }

        return Result.Ok(existingEpoch);
    }
}