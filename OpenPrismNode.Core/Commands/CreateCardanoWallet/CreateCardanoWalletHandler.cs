namespace OpenPrismNode.Core.Commands.CreateCardanoWallet;

using Entities;
using FluentResults;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Models.CardanoWallet;
using Services;

public class CreateCardanoWalletHandler : IRequestHandler<CreateCardanoWalletRequest, Result<CreateCardanoWalletResponse>>
{
    private ICardanoWalletService _walletService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CreateCardanoWalletHandler(ICardanoWalletService walletService, IServiceScopeFactory serviceScopeFactory)
    {
        _walletService = walletService;
         _serviceScopeFactory = serviceScopeFactory;
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

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;
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

        await context.WalletEntities.AddAsync(wallet, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok(new CreateCardanoWalletResponse()
        {
            Mnemonic = mnemonicList.ToList(),
            WalletId = wallet.WalletId,
            WalletEntityId = wallet.WalletEntityId
        });
    }
}