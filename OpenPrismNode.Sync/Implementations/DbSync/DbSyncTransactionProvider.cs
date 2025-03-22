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
    public async Task<Result<Metadata>> GetMetadataFromTransaction(int txId, long key, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetMetadataFromTransactionRequest(txId, (int)key), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Payment>>> GetPaymentDataFromTransaction(int txId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPaymentDataFromTransactionRequest(txId), cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail<IEnumerable<Payment>>(result.Errors);
        }
        
        // Convert single Payment to IEnumerable<Payment>
        return Result.Ok<IEnumerable<Payment>>(new[] { result.Value });
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTransactionsWithPrismMetadataForBlockIdRequest(blockId), cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail<IEnumerable<Transaction>>(result.Errors);
        }
        
        // Convert List<Transaction> to IEnumerable<Transaction>
        return Result.Ok<IEnumerable<Transaction>>(result.Value);
    }
}