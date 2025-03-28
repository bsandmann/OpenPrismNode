﻿namespace OpenPrismNode.Core.Commands.DeleteBlock;

using DeleteTransaction;
using Entities;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;

/// <summary>
/// This handler is able to delete the newest block on the chain. Delete other blocks deeper inside
/// the chain causes errors and is not supported. 
/// </summary>
public class DeleteBlockHandler : IRequestHandler<DeleteBlockRequest, Result<DeleteBlockResponse>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="mediator"></param>
    public DeleteBlockHandler(IServiceScopeFactory serviceScopeFactory, IMediator mediator)
    {
        _serviceScopeFactory = serviceScopeFactory;
        this._mediator = mediator;
    }

    public async Task<Result<DeleteBlockResponse>> Handle(DeleteBlockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var block = await context.BlockEntities
                .Select(p =>
                    new
                    {
                        blockHeight = p.BlockHeight,
                        blockHashPrefix = p.BlockHashPrefix,
                        blockHash = p.BlockHash,
                        blockEpochNumber = p.EpochNumber,
                        previousBlockHeight = p.PreviousBlockHeight,
                        previousBlockHashPrefix = p.PreviousBlockHashPrefix,
                        transactions = p.PrismTransactionEntities.Select(q => new
                        {
                            transactionhash = q.TransactionHash,
                            index = q.Index
                        }).ToList()
                    })
                .FirstOrDefaultAsync(p => p.blockHeight == request.BlockHeight && p.blockHashPrefix == request.BlockHashPrefix, cancellationToken: cancellationToken);
            if (block is not null)
            {
                try
                {
                    int? previousBlockHeight = block.previousBlockHeight;
                    int? previousBlockHashPrefix = block.previousBlockHashPrefix;
                    if (block.transactions.Any())
                    {
                        foreach (var transaction in block.transactions.OrderByDescending(p => p.index))
                        {
                            await _mediator.Send(new DeleteTransactionRequest(Hash.CreateFrom(transaction.transactionhash), block.blockHeight, block.blockHashPrefix), cancellationToken);
                        }

                        var reloadedBlock = await context.BlockEntities.FirstOrDefaultAsync(p => p.BlockHeight == request.BlockHeight && p.BlockHashPrefix == request.BlockHashPrefix, cancellationToken: cancellationToken);
                        if (reloadedBlock is null)
                        {
                            return Result.Fail($"Block {request.BlockHeight} not found");
                        }

                        context.Remove(reloadedBlock);
                    }
                    else
                    {
                        var reconstructedBlock = new BlockEntity
                        {
                            BlockHeight = block.blockHeight,
                            BlockHashPrefix = block.blockHashPrefix,
                            BlockHash = block.blockHash,
                            TimeUtc = DateTime.MinValue,
                            TxCount = 0,
                        };
                        context.Remove(reconstructedBlock);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    if (previousBlockHeight is null && previousBlockHashPrefix is null)
                    {
                        // We reached the genesis block
                        return Result.Ok(new DeleteBlockResponse(0, 0, 1));
                    }

                    return Result.Ok(new DeleteBlockResponse(previousBlockHeight!.Value, previousBlockHashPrefix!.Value, block.blockEpochNumber));
                }
                catch (Exception e)
                {
                    throw new Exception($"Error deleting block {request.BlockHeight}", e);
                }
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error deleting block {request.BlockHeight}", e);
        }

        return Result.Fail($"Block {request.BlockHeight} not found");
    }
}