namespace OpenPrismNode.Core.Commands.GetWallet;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Services;

public class GetWalletHandler : IRequestHandler<GetWalletRequest, Result<GetWalletResponse>>
{
    private ICardanoWalletService _walletService;
    private DataContext _context;

    public GetWalletHandler(ICardanoWalletService walletService, DataContext context)
    {
        _walletService = walletService;
        _context = context;
    }

    public async Task<Result<GetWalletResponse>> Handle(GetWalletRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WalletId))
        {
            return Result.Fail("Invalid walletId");
        }

        var storedWallet = await _context.WalletEntities.FirstOrDefaultAsync(p => p.WalletId == request.WalletId, cancellationToken: cancellationToken);
        if (storedWallet is null)
        {
            return Result.Fail("Wallet not found or invalid walletId");
        }

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

        _context.WalletEntities.Update(storedWallet);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok(new GetWalletResponse()
        {
            WalletEntityId = storedWallet.WalletEntityId,
            WalletId = request.WalletId,
            Balance = walletState.Value.Balance.Total.Quantity,
            SyncingComplete = walletState.Value.State.Status.Equals("ready", StringComparison.CurrentCultureIgnoreCase),
            SyncProgress = storedWallet.SyncProgress,
            FundingAddress = storedWallet.FundingAddress,
            Passphrase = storedWallet.Passphrase
        });
    }
}