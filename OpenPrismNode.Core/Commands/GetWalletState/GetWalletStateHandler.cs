namespace OpenPrismNode.Core.Commands.GetWalletState;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Services;

public class GetWalletStateHandler : IRequestHandler<GetWalletStateRequest, Result<GetWalletStateResponse>>
{
    private ICardanoWalletService _walletService;
    private DataContext _context;

    public GetWalletStateHandler(ICardanoWalletService walletService, DataContext context)
    {
        _walletService = walletService;
        _context = context;
    }

    public async Task<Result<GetWalletStateResponse>> Handle(GetWalletStateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WalletKey) || request.WalletKey.Length != 32)
        {
            return Result.Fail("Invalid wallet key");
        }

        var storedWallet = await _context.WalletEntities.FirstOrDefaultAsync(p => p.Passphrase == request.WalletKey, cancellationToken: cancellationToken);
        if (storedWallet is null)
        {
            return Result.Fail("Wallet not found or invalid wallet key");
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

        storedWallet.SyncProgress = walletState.Value.State.Progress.Quantity;
        storedWallet.LastSynced = DateTime.UtcNow;
        storedWallet.IsSyncedInitially = storedWallet.IsSyncedInitially != false || walletState.Value.State.Status.Equals("ready", StringComparison.CurrentCultureIgnoreCase);
        storedWallet.LastKnownBalance = walletState.Value.Balance.Total.Quantity;

        _context.WalletEntities.Update(storedWallet);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok(new GetWalletStateResponse()
        {
            Balance = walletState.Value.Balance.Total.Quantity,
            SyncingComplete = walletState.Value.State.Status.Equals("ready", StringComparison.CurrentCultureIgnoreCase),
            WalletKey = request.WalletKey
        });
    }
}