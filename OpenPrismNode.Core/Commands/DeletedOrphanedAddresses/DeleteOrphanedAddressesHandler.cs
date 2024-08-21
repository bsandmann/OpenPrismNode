namespace OpenPrismNode.Core.Commands.DeletedOrphanedAddresses;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DeleteOrphanedAddressesHandler : IRequestHandler<DeleteOrphanedAddressesRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public DeleteOrphanedAddressesHandler(DataContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteOrphanedAddressesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var orphanedWalletAddresses = await _context.WalletAddressEntities.Where(p => p.Utxos.Count == 0).ToListAsync(cancellationToken: cancellationToken);
            _context.WalletAddressEntities.RemoveRange(orphanedWalletAddresses);

            var orphanedStakeAddresses = await _context.StakeAddressEntities.Where(p => p.Utxos.Count == 0).ToListAsync(cancellationToken: cancellationToken);
            _context.StakeAddressEntities.RemoveRange(orphanedStakeAddresses);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting ophaned Addresses", e);
        }

        return Result.Ok();
    }
}