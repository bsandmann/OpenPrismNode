using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateWalletAddress;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

public class CreateWalletAddressHandler : IRequestHandler<CreateWalletAddressRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWalletAddressCache _cache;

    public CreateWalletAddressHandler(IServiceScopeFactory serviceScopeFactory, IWalletAddressCache cache)
    {
         _serviceScopeFactory = serviceScopeFactory;
        _cache = cache;
    }

    public async Task<Result> Handle(CreateWalletAddressRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        await _cache.GetOrAddAsync(
            request.WalletAddress,
            async () =>
            {
                var dbWalletAddress = await context.WalletAddressEntities
                    .FirstOrDefaultAsync(w => w.WalletAddress == request.WalletAddress, cancellationToken);

                if (dbWalletAddress == null)
                {
                    dbWalletAddress = new WalletAddressEntity
                    {
                        WalletAddress = request.WalletAddress
                    };

                    await context.WalletAddressEntities.AddAsync(dbWalletAddress, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                return dbWalletAddress;
            });

        return Result.Ok();
    }
}