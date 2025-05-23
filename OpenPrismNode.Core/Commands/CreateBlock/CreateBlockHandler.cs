﻿namespace OpenPrismNode.Core.Commands.CreateBlock;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

/// <summary>
/// Handler to create new blocks inside the node-database to represent a block
/// </summary>
public class CreateBlockHandler : IRequestHandler<CreateBlockRequest, Result<BlockEntity>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateBlockHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Handler
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<BlockEntity>> Handle(CreateBlockRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var existingBlock = await context.BlockEntities.AnyAsync(
            p => p.BlockHeight == request.BlockHeight && p.EpochEntity.Ledger == request.ledger,
            cancellationToken: cancellationToken);
        var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var timeCreatedUtc = DateTime.SpecifyKind(request.TimeUtc, DateTimeKind.Unspecified);

        if (!existingBlock)
        {
            var blockEntity = new BlockEntity()
            {
                BlockHeight = request.BlockHeight,
                BlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value) ?? 0,
                BlockHash = request.BlockHash.Value!,
                EpochNumber = request.EpochNumber,
                TimeUtc = timeCreatedUtc,
                TxCount = request.TxCount,
                LastParsedOnUtc = dateTimeNow,
                PreviousBlockHeight = request.PreviousBlockHeight,
                PreviousBlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.PreviousBlockHash?.Value),
                IsFork = request.IsFork,
                Ledger = request.ledger
            };

            await context.AddAsync(blockEntity, cancellationToken);

            // Update ledger LastSynced time
            await context.LedgerEntities
                .Where(n => n.Ledger == request.ledger)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.LastSynced, dateTimeNow), cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            blockEntity.TimeUtc = DateTime.SpecifyKind(blockEntity.TimeUtc, DateTimeKind.Utc);
            blockEntity.LastParsedOnUtc = blockEntity.LastParsedOnUtc is not null ? DateTime.SpecifyKind(blockEntity.LastParsedOnUtc.Value, DateTimeKind.Utc) : null;

            return Result.Ok(blockEntity);
        }
        else
        {
            // LIKELY A FORK
            try
            {
                var blockEntity = new BlockEntity()
                {
                    BlockHeight = request.BlockHeight,
                    BlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value) ?? 0,
                    BlockHash = request.BlockHash.Value,
                    EpochNumber = request.EpochNumber,
                    TimeUtc = timeCreatedUtc,
                    TxCount = request.TxCount,
                    LastParsedOnUtc = dateTimeNow,
                    PreviousBlockHeight = request.PreviousBlockHeight,
                    PreviousBlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(request.PreviousBlockHash?.Value),
                    IsFork = request.IsFork,
                    Ledger = request.ledger
                };

                await context.AddAsync(blockEntity, cancellationToken);

                // Update ledger LastSynced time
                await context.LedgerEntities
                    .Where(n => n.Ledger == request.ledger)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.LastSynced, dateTimeNow), cancellationToken);

                await context.SaveChangesAsync(cancellationToken);

                blockEntity.TimeUtc = DateTime.SpecifyKind(blockEntity.TimeUtc, DateTimeKind.Utc);
                blockEntity.LastParsedOnUtc = blockEntity.LastParsedOnUtc is not null ? DateTime.SpecifyKind(blockEntity.LastParsedOnUtc.Value, DateTimeKind.Utc) : null;

                return Result.Ok(blockEntity);
            }
            catch (Exception e)
            {
                return Result.Fail("Error while creating forked-block: " + e.Message);
            }
        }
    }
}