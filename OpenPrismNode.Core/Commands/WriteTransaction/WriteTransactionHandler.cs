namespace OpenPrismNode.Core.Commands.WriteTransaction;

using System.Text.Json;
using Common;
using CreateOperationsStatus;
using CreateWalletTransaction;
using Crypto;
using DbSyncModels;
using EncodeTransaction;
using FluentResults;
using GetWallet;
using Google.Protobuf;
using Grpc.Models;
using MediatR;
using Microsoft.Extensions.Options;
using Models;
using Models.CardanoWallet;
using Services;
using Payment = Models.CardanoWallet.Payment;

public class WriteTransactionHandler : IRequestHandler<WriteTransactionRequest, Result<WriteTransactionResponse>>
{
    private IMediator _mediator;
    private ICardanoWalletService _walletService;
    private DataContext _context;
    private ISha256Service _sha256Service;
    private readonly AppSettings _appSettings;

    public WriteTransactionHandler(ICardanoWalletService walletService, DataContext context, IMediator mediator, ISha256Service sha256Service, IOptions<AppSettings> appSettings)
    {
        _walletService = walletService;
        _context = context;
        _mediator = mediator;
        _sha256Service = sha256Service;
        _appSettings = appSettings.Value;
    }

    public async Task<Result<WriteTransactionResponse>> Handle(WriteTransactionRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var wallet = await _mediator.Send(new GetWalletRequest(request.WalletId), cancellationToken);
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

        if (wallet.Value.Balance < 1_500_000)
        {
            return Result.Fail("Insufficient funds.");
        }

        // Prepare payment (sending 1 ADA)
        var payment = new Payment()
        {
            Address = wallet.Value.FundingAddress,
            Amount = new Amount() { Quantity = 1_000_000, Unit = "lovelace" } // 1 ADA = 1,000,000 lovelace
        };


        var encodedOperation = await _mediator.Send(new EncodeTransactionRequest(new List<SignedAtalaOperation>()
        {
            request.SignedAtalaOperation
        }), cancellationToken);
        if (encodedOperation.IsFailed || encodedOperation.Value.Content is null)
        {
            return Result.Fail(encodedOperation.Errors.FirstOrDefault()?.Message);
        }

        // Prepare metadata
        var metadata = new Dictionary<string, object>
        {
            [_appSettings.MetadataKey.ToString()] = new Dictionary<string, object> { ["c"] = encodedOperation.Value.Content, ["v"] = encodedOperation.Value.Version }
        };

        var operationType = request.SignedAtalaOperation.Operation.OperationCase switch
        {
            AtalaOperation.OperationOneofCase.CreateDid => OperationTypeEnum.CreateDid,
            AtalaOperation.OperationOneofCase.UpdateDid => OperationTypeEnum.UpdateDid,
            AtalaOperation.OperationOneofCase.DeactivateDid => OperationTypeEnum.DeactivateDid,
            AtalaOperation.OperationOneofCase.ProtocolVersionUpdate => OperationTypeEnum.ProtocolVersionUpdate,
            _ => OperationTypeEnum.Unknown
        };

        var operationHash = _sha256Service.HashData(PrismEncoding.ByteStringToByteArray(request.SignedAtalaOperation.Operation.ToByteString()));
        var operationStatusId = _sha256Service.HashData(PrismEncoding.ByteStringToByteArray(request.SignedAtalaOperation.ToByteString()));

        var operationResult = await _mediator.Send(new CreateOperationStatusRequest(
            operationStatusId: operationStatusId,
            operationHash: operationHash,
            status: OperationStatusEnum.PendingSubmission,
            operationType: operationType
        ), cancellationToken);
        if (operationResult.IsFailed)
        {
            return Result.Fail(operationResult.Errors.FirstOrDefault()?.Message);
        }

        var passphrase = wallet.Value.Passphrase;
        var transactionResult = await _walletService.CreateAndSubmitTransactionAsync(wallet.Value.WalletId, passphrase, payment, metadata);
        if (transactionResult.IsFailed)
        {
            return Result.Fail(transactionResult.Errors.FirstOrDefault()?.Message);
        }

        var createWalletTransactionResult = await _mediator.Send(
            new CreateWalletTransactionEntityRequest(
                wallet.Value.WalletEntityId,
                transactionResult.Value,
                operationResult.Value.OperationStatusEntityId
            ), cancellationToken);
        
        if (createWalletTransactionResult.IsFailed)
        {
            return Result.Fail(createWalletTransactionResult.Errors.FirstOrDefault()?.Message);
        }

        return Result.Ok(new WriteTransactionResponse()
        {
            OperationStatusId = operationResult.Value.OperationStatusId,
            OperationType = operationType,
            DidSuffix = operationType == OperationTypeEnum.CreateDid ? PrismEncoding.ByteArrayToHex(operationHash) : null
        });
    }
}