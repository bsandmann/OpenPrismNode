namespace OpenPrismNode.Core.Commands.GetOperationStatus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using OpenPrismNode.Core.Entities;
    using Services;
    using UpdateOperationStatus;
    using UpdateWalletTransaction;

    public class GetOperationStatusHandler : IRequestHandler<GetOperationStatusRequest, Result<OperationStatusEntity>>
    {
        private ICardanoWalletService _walletService;
        private readonly DataContext _context;
        private readonly IMediator _mediator;

        public GetOperationStatusHandler(DataContext context, ICardanoWalletService walletService, IMediator mediator)
        {
            _context = context;
            _walletService = walletService;
            _mediator = mediator;
        }

        public async Task<Result<OperationStatusEntity>> Handle(GetOperationStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var operationStatus = await _context.OperationStatusEntities
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
                        // CreateDidEntity = p.CreateDidEntity != null ? new CreateDidEntity() { OperationHash = p.CreateDidEntity.OperationHash } : null,
                        // UpdateDidEntity = p.UpdateDidEntity != null ? new UpdateDidEntity() { OperationHash = p.UpdateDidEntity.OperationHash } : null,
                        // DeactivateDidEntity = p.DeactivateDidEntity != null ? new DeactivateDidEntity() { OperationHash = p.DeactivateDidEntity.OperationHash } : null,
                    })
                    .FirstOrDefaultAsync(os => os.OperationStatusId == request.OperationStatusId, cancellationToken);

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

                if (transactionDetails.Value.Status == "pending" && operationStatus.Status != OperationStatusEnum.PendingSubmission)
                {
                    await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.PendingSubmission), cancellationToken);
                }
                else if (operationStatus.Status == OperationStatusEnum.PendingSubmission ||
                    operationStatus.Status == OperationStatusEnum.AwaitConfirmation)
                {
                    // TODO define a requirement for the depth of the transaction
                    if (transactionDetails.Value.Depth.Quantity <= 2 && operationStatus.Status == OperationStatusEnum.PendingSubmission)
                    {
                        await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.AwaitConfirmation), cancellationToken);
                    }
                    else if (transactionDetails.Value.Depth.Quantity > 2 && transactionDetails.Value.Status.Equals("in_ledger", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await _mediator.Send(new UpdateOperationStatusRequest(operationStatus.OperationStatusEntityId, OperationStatusEnum.ConfirmedAndApplied), cancellationToken);
                    }
                }

                await _mediator.Send(new UpdateWalletTransactionRequest(
                        walletTransactionEntityId: operationStatus.WalletTransactionEntity.WalletTransactionEntityId,
                        depth: transactionDetails.Value.Depth.Quantity,
                        fee: transactionDetails.Value.Fee.Quantity)
                    , cancellationToken);

                return Result.Ok(new OperationStatusEntity()
                {
                    OperationStatusId = operationStatus.OperationStatusId,
                    Status = operationStatus.Status,
                    OperationHash = operationStatus.OperationHash,
                    OperationType = operationStatus.OperationType,
                    CreatedUtc = operationStatus.CreatedUtc,
                    LastUpdatedUtc = operationStatus.LastUpdatedUtc,
                });
            }
            catch (Exception ex)
            {
                return Result.Fail("Failed to retrieve operation status");
            }
        }
    }
}