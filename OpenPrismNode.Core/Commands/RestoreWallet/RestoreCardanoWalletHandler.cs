namespace OpenPrismNode.Core.Commands.RestoreWallet;

using CreateCardanoWallet;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models.CardanoWallet;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Services;

public class RestoreCardanoWalletHandler : IRequestHandler<RestoreCardanoWalletRequest, Result<RestoreCardanoWalletResponse>>
{
    private ICardanoWalletService _walletService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RestoreCardanoWalletHandler(ICardanoWalletService walletService, IServiceScopeFactory serviceScopeFactory)
    {
        _walletService = walletService;
         _serviceScopeFactory = serviceScopeFactory;
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

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var existingWallet = await context.WalletEntities
            .FirstOrDefaultAsync(w => w.WalletId == walletResponse.Value.Id, cancellationToken);

        if (existingWallet != null)
        {
            return Result.Ok(new RestoreCardanoWalletResponse()
            {
                WalletId = existingWallet.WalletId,
            });
        }

        // Write the wallet to the database
        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;
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

        await context.WalletEntities.AddAsync(wallet, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok(new RestoreCardanoWalletResponse()
        {
            WalletId = wallet.WalletId,
        });
    }
}