namespace OpenPrismNode.Core.Commands.Withdrawal;

using FluentResults;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Commands.CreateOperationsStatus;
using OpenPrismNode.Core.Commands.CreateWalletTransaction;
using OpenPrismNode.Core.Commands.EncodeTransaction;
using OpenPrismNode.Core.Commands.GetWallet;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Models.CardanoWallet;
using OpenPrismNode.Core.Services;
using WriteTransaction;
using Payment = Models.CardanoWallet.Payment;

public class WithdrawalHandler : IRequestHandler<WithdrawalRequest, Result>
{
    private IMediator _mediator;
    private ICardanoWalletService _walletService;
    private DataContext _context;
    private ISha256Service _sha256Service;
    private readonly AppSettings _appSettings;

    public WithdrawalHandler(ICardanoWalletService walletService, DataContext context, IMediator mediator, ISha256Service sha256Service, IOptions<AppSettings> appSettings)
    {
        _walletService = walletService;
        _context = context;
        _mediator = mediator;
        _sha256Service = sha256Service;
        _appSettings = appSettings.Value;
    }

    public async Task<Result> Handle(WithdrawalRequest request, CancellationToken cancellationToken)
    {
        var wallet = await _mediator.Send(new GetWalletRequest { WalletId = request.WalletId }, cancellationToken);
        if (wallet.IsFailed)
        {
            return Result.Fail(wallet.Errors.FirstOrDefault()?.Message);
        }

        if (wallet.Value.SyncingComplete == false)
        {
            return Result.Fail("Wallet is not fully synced.");
        }

        if (wallet.Value.FundingAddress is null)
        {
            return Result.Fail("Funding address not found.");
        }

        if (wallet.Value.Balance == 0)
        {
            return Result.Fail("Nothing to withdraw");
        }

        // Prepare payment (sending 1 ADA)
        var payment = new Payment()
        {
            Address = request.WithdrawalAddress,
            Amount = new Amount() { Quantity = wallet.Value.Balance, Unit = "lovelace" }
        };

        var passphrase = wallet.Value.Passphrase;
        var transactionResult = await _walletService.CreateAndSubmitTransactionAsync(wallet.Value.WalletId, passphrase, payment, null);
        if (transactionResult.IsFailed)
        {
            return Result.Fail(transactionResult.Errors.FirstOrDefault()?.Message);
        }

        return Result.Ok();
    }
}