namespace OpenPrismNode.Core.Commands.DeletedOrphanedAddresses;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class DeleteOrphanedAddressesHandler : IRequestHandler<DeleteOrphanedAddressesRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    public DeleteOrphanedAddressesHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteOrphanedAddressesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var orphanedWalletAddresses = await context.WalletAddressEntities.Where(p => p.Utxos.Count == 0).ToListAsync(cancellationToken: cancellationToken);
            context.WalletAddressEntities.RemoveRange(orphanedWalletAddresses);

            var orphanedStakeAddresses = await context.StakeAddressEntities.Where(p => p.Utxos.Count == 0).ToListAsync(cancellationToken: cancellationToken);
            context.StakeAddressEntities.RemoveRange(orphanedStakeAddresses);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting ophaned Addresses", e);
        }

        return Result.Ok();
    }
}