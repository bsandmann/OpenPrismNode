// For GetWalletResponse

namespace OpenPrismNode.Core.Commands.GetWalletByOperationStatus
{
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using OpenPrismNode.Core.Commands.GetWallet;
    using OpenPrismNode.Core.Entities;
    using OpenPrismNode.Core.Services;

    public class GetWalletByOperationStatusIdHandler
        : IRequestHandler<GetWalletByOperationStatusIdRequest, Result<GetWalletResponse?>>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ICardanoWalletService _walletService;

        public GetWalletByOperationStatusIdHandler(
            IServiceScopeFactory serviceScopeFactory,
            ICardanoWalletService walletService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _walletService = walletService;
        }

        public async Task<Result<GetWalletResponse?>> Handle(
            GetWalletByOperationStatusIdRequest request, 
            CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Read-only => no change tracking updates
            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                var operationStatus = await context.OperationStatusEntities
                    .Include(os => os.WalletTransactionEntity)
                        .ThenInclude(wtx => wtx.Wallet)
                    .FirstOrDefaultAsync(
                        os => os.OperationStatusId == request.OperationStatusId,
                        cancellationToken);

                // 2) If no matching operation or no associated wallet, return Ok(null)
                if (operationStatus?.WalletTransactionEntity?.Wallet is not WalletEntity walletEntity 
                    || string.IsNullOrWhiteSpace(walletEntity.WalletId))
                {
                    return Result.Ok<GetWalletResponse?>(null);
                }

                // 3) Retrieve the latest wallet state from the external service
                //    If it fails, or if the wallet status is invalid, return Ok(null).
                var walletStateResult = await _walletService.GetWalletAsync(walletEntity.WalletId);
                if (walletStateResult.IsFailed)
                {
                    // Not a DB or strict data-conversion error => return Ok(null)
                    return Result.Ok<GetWalletResponse?>(null);
                }

                var walletState = walletStateResult.Value;

                // 4) Check the currency unit (data-conversion check => fail if invalid)
                if (!walletState.Balance.Total.Unit.Equals("lovelace", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Result.Fail<GetWalletResponse?>(
                        "Unexpected unit in wallet balance. Should be in lovelace.");
                }

                // If the wallet is not "syncing" or "ready", treat it as domain mismatch => Ok(null)
                if (!walletState.State.Status.Equals("syncing", StringComparison.InvariantCultureIgnoreCase)
                    && !walletState.State.Status.Equals("ready", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Result.Ok<GetWalletResponse?>(null);
                }

                // 5) Build a response from the DB + external read data 
                //    (No DB writes, purely read)
                var response = new GetWalletResponse
                {
                    WalletEntityId = walletEntity.WalletEntityId,
                    WalletId       = walletEntity.WalletId,
                    // External read:
                    Balance = walletState.Balance.Total.Quantity,
                    // "SyncingComplete" is true if status is "ready"
                    SyncingComplete = walletState.State.Status
                        .Equals("ready", StringComparison.InvariantCultureIgnoreCase),
                    // If "ready", assume 100% progress; otherwise use the external progress if available
                    SyncProgress    = walletState.State.Status
                        .Equals("ready", StringComparison.InvariantCultureIgnoreCase)
                        ? 100
                        : walletState.State.Progress?.Quantity,
                    // From DB as-is (no changes)
                    FundingAddress  = walletEntity.FundingAddress,
                    Passphrase      = walletEntity.Passphrase
                };

                // Return a successful result with the read-only data
                return Result.Ok<GetWalletResponse?>(response);
            }
            catch (DbUpdateException dbEx)
            {
                // Database-related error => fail
                return Result.Fail<GetWalletResponse?>(
                    new Error("Database error while retrieving wallet by OperationStatusId.")
                        .CausedBy(dbEx));
            }
            catch (Exception)
            {
                // All other (non-DB, non-data-conversion) exceptions => Ok(null)
                return Result.Ok<GetWalletResponse?>(null);
            }
        }
    }
}
