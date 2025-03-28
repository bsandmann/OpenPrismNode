﻿namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockId;

using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves a specific block from the Cardano DB Sync PostgreSQL database by its internal database ID.
/// This handler is used primarily when following chains of blocks through previous_id references.
/// </summary>
public class GetPostgresBlockByBlockIdHandler : IRequestHandler<GetPostgresBlockByBlockIdRequest, Result<Block>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresBlockByBlockIdHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<Block>> Handle(GetPostgresBlockByBlockIdRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            // SQL Query: Retrieves a block by its internal database ID
            // - Selects core block information (id, hash, epoch, block number, time, tx count, etc.)
            // - Filters by the database ID (primary key in the block table)
            // - WARNING: This uses string interpolation and is susceptible to SQL injection
            //   This should be changed to use parameterized queries
            string commandText = $"  SELECT id, hash, epoch_no, block_no, time, tx_count, previous_id FROM public.block WHERE id = {request.BlockId};";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText);
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }

            block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            return Result.Ok(block);
        }
    }
}