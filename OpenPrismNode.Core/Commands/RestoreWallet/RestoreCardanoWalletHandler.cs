namespace OpenPrismNode.Core.Commands.RestoreWallet;

using CreateCardanoWallet;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Models.CardanoWallet;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Services;

public class RestoreCardanoWalletHandler : IRequestHandler<RestoreCardanoWalletRequest, Result<RestoreCardanoWalletResponse>>
{
    private ICardanoWalletService _walletService;
    private DataContext _context;

    public RestoreCardanoWalletHandler(ICardanoWalletService walletService, DataContext context)
    {
        _walletService = walletService;
        _context = context;
    }

    public async Task<Result<RestoreCardanoWalletResponse>> Handle(RestoreCardanoWalletRequest request, CancellationToken cancellationToken)
    {
        if (request.Mnemonic.Count != 24)
        {
            return Result.Fail("Invalid mnemonic length. Expected 24 words.");
        }

        if (!request.Mnemonic.TrueForAll(p => !string.IsNullOrWhiteSpace(p)))
        {
            return Result.Fail("Invalid mnemonic. All words must be non-empty.");
        }

        var passphrase = _walletService.GeneratePassphrase(32);

        var createWalletRequest = new CreateWalletRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Default Wallet" : request.Name,
            Passphrase = passphrase,
            MnemonicSentence = request.Mnemonic.ToArray(),
            RestorationMode = "from_genesis"
        };

        var walletResponse = await _walletService.CreateWalletAsync(createWalletRequest);
        if (walletResponse.IsFailed)
        {
            return walletResponse.ToResult();
        }

        var existingWallet = await _context.WalletEntities
            .FirstOrDefaultAsync(w => w.WalletId == walletResponse.Value.Id, cancellationToken);

        if (existingWallet != null)
        {
            return Result.Ok(new RestoreCardanoWalletResponse()
            {
                WalletId = existingWallet.WalletId,
            });
        }

        // Write the wallet to the database
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var wallet = new WalletEntity()
        {
            Passphrase = passphrase,
            Mnemonic = string.Join(" ", request.Mnemonic),
            CreatedUtc = DateTime.UtcNow,
            LastSynced = null,
            FundingAddress = null,
            SyncProgress = null,
            WalletName = walletResponse.Value.Name,
            WalletId = walletResponse.Value.Id,
            WalletTransactions = new List<WalletTransactionEntity>(),
            IsInSync = null,
            IsSyncedInitially = false,
            LastKnownBalance = null,
        };

        await _context.WalletEntities.AddAsync(wallet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok(new RestoreCardanoWalletResponse()
        {
            WalletId = wallet.WalletId,
        });
    }
}