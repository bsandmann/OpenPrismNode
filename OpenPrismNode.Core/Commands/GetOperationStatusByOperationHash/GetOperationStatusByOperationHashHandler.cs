namespace OpenPrismNode.Core.Commands.GetOperationStatusByOperationHash
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using OpenPrismNode.Core.Commands.UpdateOperationStatus;
    using OpenPrismNode.Core.Commands.UpdateWalletTransaction;
    using OpenPrismNode.Core.Common;
    using OpenPrismNode.Core.Models;
    using OpenPrismNode.Core.Services;

    public class GetOperationStatusByOperationHashHandler : IRequestHandler<GetOperationStatusByOperationHashRequest, Result<GetOperationStatusByOperationHashResponse>>
    {
        private ICardanoWalletService _walletService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMediator _mediator;
        private readonly AppSettings _appSettings;

        public GetOperationStatusByOperationHashHandler(IServiceScopeFactory serviceScopeFactory, ICardanoWalletService walletService, IMediator mediator, IOptions<AppSettings> appSettings)
        {
             _serviceScopeFactory = serviceScopeFactory;
            _walletService = walletService;
            _mediator = mediator;
            _appSettings = appSettings.Value;
        }

        public async Task<Result<GetOperationStatusByOperationHashResponse>> Handle(GetOperationStatusByOperationHashRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                context.ChangeTracker.Clear();
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                var operationStatus = await context.OperationStatusEntities
                    .Select(p => new
                    {
                        OperationStatusEntityId = p.OperationStatusEntityId,
                        OperationStatusId = p.OperationStatusId,
                        Status = p.Status,
                        OperationHash = p.OperationHash,
                        OperationType = p.OperationType,
                        CreatedUtc = p.CreatedUtc,
                        LastUpdatedUtc = p.LastUpdatedUtc,
                        WalletTransactionEntity = p.WalletTransactionEntity,
                        WalletId = p.WalletTransactionEntity != null ? p.WalletTransactionEntity.Wallet.WalletId : null,
                    })
                    .FirstOrDefaultAsync(os => os.OperationHash == request.OperationHash, cancellationToken);

                if (operationStatus == null)
                {
                    return Result.Fail("OperationStatus not found.");
                }

                // We check the operationStatus on the blockchain
                if (operationStatus.WalletTransactionEntity is null)
                {
                    return Result.Fail("OperationStatus does not have a WalletTransactionEntity");
                }

                var transactionId = operationStatus.WalletTransactionEntity.TransactionId;
                if (string.IsNullOrWhiteSpace(operationStatus.WalletId))
                {
                    return Result.Fail("OperationStatus does not have a WalletId");
                }

                var transactionDetails = await _walletService.GetTransactionDetailsAsync(operationStatus.WalletId, transactionId);
                if (transactionDetails.IsFailed)
                {
                    return transactionDetails.ToResult();
                }

                var requiredConfirmationDepth = _appSettings.RequiredConfirmationDepth ?? 2;

                var status = operationStatus.Status;
                if (transactionDetails.Value.Status.Equals("pending") && status != OperationStatusEnum.PendingSubmission)
                {
                    await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.PendingSubmission), cancellationToken);
                    status = OperationStatusEnum.PendingSubmission;
                }
                else if ((status == OperationStatusEnum.PendingSubmission ||
                          status == OperationStatusEnum.AwaitConfirmation) && !transactionDetails.Value.Status.Equals("pending"))
                {
                    if (transactionDetails.Value.Depth?.Quantity < requiredConfirmationDepth && status != OperationStatusEnum.AwaitConfirmation)
                    {
                        await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.AwaitConfirmation), cancellationToken);
                        status = OperationStatusEnum.AwaitConfirmation;
                    }
                    else if (transactionDetails.Value.Depth?.Quantity >= requiredConfirmationDepth && transactionDetails.Value.Status.Equals("in_ledger", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.ConfirmedAndApplied), cancellationToken);
                        status = OperationStatusEnum.ConfirmedAndApplied;
                    }
                }

                if (!transactionDetails.Value.Status.Equals("pending"))
                {
                    await _mediator.Send(new UpdateWalletTransactionRequest(
                            walletTransactionEntityId: operationStatus.WalletTransactionEntity.WalletTransactionEntityId,
                            depth: transactionDetails.Value.Depth?.Quantity ?? 0,
                            fee: transactionDetails.Value.Fee.Quantity)
                        , cancellationToken);
                }

                return Result.Ok(new GetOperationStatusByOperationHashResponse()
                {
                    OperationStatusId = operationStatus.OperationStatusId,
                    Status = status,
                    OperationHash = operationStatus.OperationHash,
                    OperationType = operationStatus.OperationType,
                    CreatedUtc = operationStatus.CreatedUtc,
                    LastUpdatedUtc = operationStatus.LastUpdatedUtc,
                    TransactionId = transactionId
                });
            }
            catch (Exception ex)
            {
                return Result.Fail("Failed to retrieve operation status");
            }
        }
    }
}