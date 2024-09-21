namespace OpenPrismNode.Core.Commands.UpdateWalletTransaction;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UpdateOperationStatus;

public class UpdateWalletTransactionHandler : IRequestHandler<UpdateWalletTransactionRequest,Result>
{
    private readonly DataContext _context;

    public UpdateWalletTransactionHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateWalletTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var walletTransaction = await _context.WalletTransactionEntities
                .FirstOrDefaultAsync(os => os.WalletTransactionEntityId == request.WalletTransactionEntityId, cancellationToken);

            if (walletTransaction == null)
            {
                return Result.Fail("Wallet Transaction not found.");
            }

            walletTransaction.Depth = request.Depth;
            walletTransaction.Fee = request.Fee;
            walletTransaction.LastUpdatedUtc = DateTime.UtcNow;
            
            _context.WalletTransactionEntities.Update(walletTransaction);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to update Wallet-Transaction").CausedBy(ex));
        }
    }
}