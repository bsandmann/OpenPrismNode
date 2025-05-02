namespace OpenPrismNode.Sync.Implementations.DbSync;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Models;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Commands.DbSync.GetNextBlockWithPrismMetadata;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockId;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockNo;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockTip;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlocksByBlockNos;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresFirstBlockOfEpoch;

/// <summary>
/// Implementation of the IBlockProvider interface that retrieves data from a Cardano DB Sync PostgreSQL database.
/// </summary>
public class DbSyncBlockProvider : IBlockProvider
{
    private readonly IMediator _mediator;

    public DbSyncBlockProvider(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetPostgresBlockTipRequest(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetPostgresBlockByBlockNoRequest(blockNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken, int? blockNo = null)
    {
        return await _mediator.Send(new GetPostgresBlockByBlockIdRequest(blockId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<List<Block>>> GetBlocksByNumbers(int firstBlockNo, int count, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPostgresBlocksByBlockNosRequest(firstBlockNo, count), cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        // Return the blocks as IEnumerable<Block>
        return Result.Ok(result.Value.ToList());
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetPostgresFirstBlockOfEpochRequest(epochNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<GetNextBlockWithPrismMetadataResponse>> GetNextBlockWithPrismMetadata(int afterBlockNo, int maxBlockNo, LedgerType ledgerType, int metadataKey, int currentBlockTip, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetNextBlockWithPrismMetadataRequest(afterBlockNo, metadataKey, maxBlockNo, ledgerType), cancellationToken);
    }
}