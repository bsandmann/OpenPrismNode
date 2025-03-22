namespace OpenPrismNode.Sync.Implementations.DbSync;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetPostgresBlockTipRequest(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetPostgresBlockByBlockNoRequest(blockNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetPostgresBlockByBlockIdRequest(blockId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Block>>> GetBlocksByNumbers(IEnumerable<int> blockNos, CancellationToken cancellationToken = default)
    {
        // For simplicity, just get the first block number and count - this may need to be adjusted
        // based on how the actual request handler works
        var firstBlockNo = blockNos.Min();
        var count = blockNos.Count();
        var result = await _mediator.Send(new GetPostgresBlocksByBlockNosRequest(firstBlockNo, count), cancellationToken);
        
        // Convert List<Block> to IEnumerable<Block>
        return result.ToResult<IEnumerable<Block>>();
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetPostgresFirstBlockOfEpochRequest(epochNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetNextBlockWithPrismMetadata(int afterBlockNo, CancellationToken cancellationToken = default)
    {
        // This may need adjustments based on actual parameters expected by the handler
        var result = await _mediator.Send(new GetNextBlockWithPrismMetadataRequest(afterBlockNo, 21325, Int32.MaxValue, Core.Models.LedgerType.CardanoPreprod), cancellationToken);
        
        if (result.IsFailed)
        {
            return Result.Fail<Block>(result.Errors);
        }
        
        // If result has BlockHeight, get that block
        if (result.Value.BlockHeight.HasValue)
        {
            return await GetBlockByNumber(result.Value.BlockHeight.Value, cancellationToken);
        }
        
        // No block with PRISM metadata found
        return Result.Fail<Block>("No block with PRISM metadata found after the specified block number");
    }
}