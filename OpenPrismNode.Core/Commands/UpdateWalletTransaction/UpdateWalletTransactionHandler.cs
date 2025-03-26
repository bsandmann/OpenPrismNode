namespace OpenPrismNode.Core.Commands.UpdateWalletTransaction;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdateOperationStatus;

public class UpdateWalletTransactionHandler : IRequestHandler<UpdateWalletTransactionRequest,Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UpdateWalletTransactionHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result> Handle(UpdateWalletTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var walletTransaction = await context.WalletTransactionEntities
                .FirstOrDefaultAsync(os => os.WalletTransactionEntityId == request.WalletTransactionEntityId, cancellationToken);

            if (walletTransaction == null)
            {
                return Result.Fail("Wallet Transaction not found.");
            }

            walletTransaction.Depth = request.Depth;
            walletTransaction.Fee = request.Fee;
            walletTransaction.LastUpdatedUtc = DateTime.UtcNow;

            context.WalletTransactionEntities.Update(walletTransaction);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to update Wallet-Transaction").CausedBy(ex));
        }
    }
}