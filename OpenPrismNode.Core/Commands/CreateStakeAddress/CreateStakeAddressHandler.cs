using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateStakeAddress;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

public class CreateStakeAddressHandler : IRequestHandler<CreateStakeAddressRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IStakeAddressCache _cache;

    public CreateStakeAddressHandler(IServiceScopeFactory serviceScopeFactory, IStakeAddressCache cache, ILogger<CreateStakeAddressHandler> logger)
    {
         _serviceScopeFactory = serviceScopeFactory;
        _cache = cache;
    }

    public async Task<Result> Handle(CreateStakeAddressRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        await _cache.GetOrAddAsync(
            request.StakeAddress,
            async () =>
            {
                var dbStakeAddress = await context.StakeAddressEntities
                    .FirstOrDefaultAsync(w => w.StakeAddress == request.StakeAddress, cancellationToken);

                if (dbStakeAddress == null)
                {
                    dbStakeAddress = new StakeAddressEntity()
                    {
                        StakeAddress = request.StakeAddress
                    };

                    await context.StakeAddressEntities.AddAsync(dbStakeAddress, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                return dbStakeAddress;
            });

        return Result.Ok();
    }
}