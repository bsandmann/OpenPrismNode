namespace OpenPrismNode.Core.Commands.GetWallets;

using FluentResults;
using GetWallet;
using GetWallets;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Services;

public class GetWalletsHandler : IRequestHandler<GetWalletsRequest, Result<List<GetWalletResponse>>>
{
    private ICardanoWalletService _walletService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetWalletsHandler(ICardanoWalletService walletService, IServiceScopeFactory serviceScopeFactory)
    {
        _walletService = walletService;
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<List<GetWalletResponse>>> Handle(GetWalletsRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var storedWallets = await context.WalletEntities.ToListAsync(cancellationToken: cancellationToken);
        if (!storedWallets.Any())
        {
            return Result.Fail("No wallets not found");
        }

        var responses = new List<GetWalletResponse>();
        foreach (var storedWallet in storedWallets.OrderBy(p=>p.CreatedUtc))
        {
            var walletState = await _walletService.GetWalletAsync(storedWallet.WalletId);
            if (walletState.IsFailed)
            {
                return Result.Fail(walletState.Errors.FirstOrDefault()?.Message);
            }

            if (!walletState.Value.Balance.Total.Unit.Equals("lovelace", StringComparison.InvariantCultureIgnoreCase))
            {
                return Result.Fail("Unexpected unit in wallet balance. Should be in lovelace.");
            }

            if (!walletState.Value.State.Status.Equals("syncing") && !walletState.Value.State.Status.Equals("ready"))
            {
                return Result.Fail("Wallet is not in a syncing or ready state.");
            }

            storedWallet.LastSynced = DateTime.UtcNow;
            storedWallet.IsSyncedInitially = storedWallet.IsSyncedInitially != false || walletState.Value.State.Status.Equals("ready", StringComparison.CurrentCultureIgnoreCase);
            storedWallet.LastKnownBalance = walletState.Value.Balance.Total.Quantity;
            if (!walletState.Value.State.Status.Equals("ready", StringComparison.InvariantCultureIgnoreCase) && walletState.Value.State.Progress is not null)
            {
                storedWallet.SyncProgress = walletState.Value.State.Progress.Quantity;
            }
            else if (walletState.Value.State.Status.Equals("ready", StringComparison.InvariantCultureIgnoreCase))
            {
                storedWallet.SyncProgress = 100;
            }

            if (storedWallet.FundingAddress is null && walletState.Value.State.Status.Equals("ready", StringComparison.InvariantCultureIgnoreCase))
            {
                var walletAddresses = await _walletService.ListAddressesAsync(storedWallet.WalletId);
                if (walletAddresses.IsFailed || !walletAddresses.Value.Any())
                {
                    return Result.Fail("Failed to get wallet addresses");
                }

                var intialFundingAddress = walletAddresses.Value.First(p => p.State == "unused");
                storedWallet.FundingAddress = intialFundingAddress.Id;
            }

            context.WalletEntities.Update(storedWallet);
            await context.SaveChangesAsync(cancellationToken);

            responses.Add(new GetWalletResponse()
            {
                WalletEntityId = storedWallet.WalletEntityId,
                WalletId = storedWallet.WalletId,
                Balance = walletState.Value.Balance.Total.Quantity,
                SyncingComplete = walletState.Value.State.Status.Equals("ready", StringComparison.CurrentCultureIgnoreCase),
                SyncProgress = storedWallet.SyncProgress,
                FundingAddress = storedWallet.FundingAddress,
                Passphrase = storedWallet.Passphrase
            });
        }

        return Result.Ok(responses);
    }
}