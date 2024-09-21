namespace OpenPrismNode.Core.Commands.CreateWalletTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;

public class CreateWalletTransactionEntityHandler : IRequestHandler<CreateWalletTransactionEntityRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateWalletTransactionEntityHandler(DataContext context)
    {
        this._context = context;
    }


    public async Task<Result> Handle(CreateWalletTransactionEntityRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionId))
        {
            return Result.Fail("TransactionId is required");
        }

        if (request.WalletEntityId == 0)
        {
            return Result.Fail("WalletId is required");
        }

        if (request.OperationStatusEntityId == 0)
        {
            return Result.Fail("OperationStatusEntityId is required");
        }

        await _context.WalletTransactionEntities.AddAsync(new WalletTransactionEntity()
        {
            OperationStatusEntityId = request.OperationStatusEntityId,
            // OperationStatusId = request.OperationStatusId,
            TransactionId = request.TransactionId,
            CreatedUtc = DateTime.UtcNow,
            WalletEntityId = request.WalletEntityId,
            Depth = 0,
            LastUpdatedUtc = DateTime.UtcNow
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}