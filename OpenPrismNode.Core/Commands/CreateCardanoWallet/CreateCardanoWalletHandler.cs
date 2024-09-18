namespace OpenPrismNode.Core.Commands.CreateCardanoWallet;

using Entities;
using FluentResults;
using MediatR;
using Services;

public class CreateCardanoWalletHandler : IRequestHandler<CreateCardanoWalletRequest, Result<CreateCardanoWalletResponse>>
{
    private ICardanoWalletService _walletService;
    private DataContext _context;

    public CreateCardanoWalletHandler(ICardanoWalletService walletService, DataContext context)
    {
        _walletService = walletService;
        _context = context;
    }

    public async Task<Result<CreateCardanoWalletResponse>> Handle(CreateCardanoWalletRequest request, CancellationToken cancellationToken)
    {
        // Create the wallet on the cardano-wallet-webapi
        var mnemonic = _walletService.GenerateMnemonic();
        var passphrase = _walletService.GeneratePassphrase(32);

        var mnemonicList = mnemonic.Split(" ");
        var createWalletRequest = new CreateWalletRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Default Wallet" : request.Name,
            Passphrase = passphrase,
            MnemonicSentence = mnemonicList
        };

        var walletResponse = await _walletService.CreateWalletAsync(createWalletRequest);
        if (walletResponse.IsFailed)
        {
            return walletResponse.ToResult();
        }

        // Write the wallet to the database
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var wallet = new WalletEntity()
        {
            Passphrase = passphrase,
            Mnemonic = mnemonic,
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

        return Result.Ok(new CreateCardanoWalletResponse()
        {
            Mnemonic = mnemonicList.ToList(),
            WalletId = wallet.WalletId,
            WalletKey = passphrase
        });
    }
}