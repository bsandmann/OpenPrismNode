namespace OpenPrismNode.Sync.Implementations.DbSync;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Commands.DbSync.GetMetadataFromTransaction;
using OpenPrismNode.Sync.Commands.DbSync.GetPaymentDataFromTransaction;
using OpenPrismNode.Sync.Commands.DbSync.GetTransactionsWithPrismMetadataForBlockId;

/// <summary>
/// Implementation of the ITransactionProvider interface that retrieves data from a Cardano DB Sync PostgreSQL database.
/// </summary>
public class DbSyncTransactionProvider : ITransactionProvider
{
    private readonly IMediator _mediator;

    public DbSyncTransactionProvider(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<Result<Metadata>> GetMetadataFromTransaction(int txId, byte[] txHash, long key, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetMetadataFromTransactionRequest(txId, null, (int)key), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Payment>> GetPaymentDataFromTransaction(int txId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPaymentDataFromTransactionRequest(txId), cancellationToken);
        return result.ToResult();
    }

    /// <inheritdoc />
    public async Task<Result<List<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, int blockNo, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTransactionsWithPrismMetadataForBlockIdRequest(blockId), cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail<List<Transaction>>(result.Errors);
        }

        // Convert List<Transaction> to IEnumerable<Transaction>
        return Result.Ok<List<Transaction>>(result.Value);
    }
}