using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Sync.Commands.ProcessBlock;

namespace OpenPrismNode.Sync.Commands.SwitchBranch;

using DbSync.GetPostgresBlockByBlockNo;
using Microsoft.Extensions.DependencyInjection;

public class SwitchBranchHandler : IRequestHandler<SwitchBranchRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMediator _mediator;

    public SwitchBranchHandler(IMediator mediator, IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
        _mediator = mediator;
    }

    public async Task<Result> Handle(SwitchBranchRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            // Find the base block
            var baseBlock = await context.BlockEntities
                .FirstOrDefaultAsync(b => b.BlockHeight == request.BaseBlockHeight && b.BlockHashPrefix == request.BaseBlockPrefix && b.Ledger == request.Ledger, cancellationToken);

            if (baseBlock == null)
            {
                return Result.Fail($"Base block not found at height {request.BaseBlockHeight} with prefix {request.BaseBlockPrefix}");
            }

            // Find the new tip block
            var newTipBlock = await context.BlockEntities
                .FirstOrDefaultAsync(b => b.BlockHeight == request.NewTipBlockHeight && b.BlockHashPrefix == request.NewTipBlockPrefix && b.Ledger == request.Ledger, cancellationToken);

            if (newTipBlock == null)
            {
                return Result.Fail($"New tip block not found at height {request.NewTipBlockHeight} with prefix {request.NewTipBlockPrefix}");
            }

            // Get all blocks after the base block
            var blocksAfterBase = await context.BlockEntities
                .Where(b => b.BlockHeight > baseBlock.BlockHeight && b.Ledger == request.Ledger)
                .ToListAsync(cancellationToken);

            // Find the path from new tip to base block
            var newMainChain = new HashSet<(int Height, int Prefix)>();
            var currentBlock = newTipBlock;
            while (currentBlock.BlockHeight > baseBlock.BlockHeight)
            {
                newMainChain.Add((currentBlock.BlockHeight, currentBlock.BlockHashPrefix));
                currentBlock = await context.BlockEntities
                    .FirstOrDefaultAsync(b => b.BlockHeight == currentBlock.PreviousBlockHeight && b.BlockHashPrefix == currentBlock.PreviousBlockHashPrefix && b.Ledger == request.Ledger, cancellationToken);

                if (currentBlock == null)
                {
                    return Result.Fail($"Chain broken at height {currentBlock.PreviousBlockHeight}");
                }
            }

            // Update fork flags
            var blocksToUpdate = new List<BlockEntity>();
            foreach (var block in blocksAfterBase)
            {
                bool shouldBeFork = !newMainChain.Contains((block.BlockHeight, block.BlockHashPrefix));

                if (block.IsFork != shouldBeFork)
                {
                    // Only update if the flag needs to change
                    block.IsFork = shouldBeFork;
                    blocksToUpdate.Add(block);
                }
            }

            // Bulk update the changed blocks
            if (blocksToUpdate.Any())
            {
                context.BlockEntities.AttachRange(blocksToUpdate);
                foreach (var block in blocksToUpdate)
                {
                    context.Entry(block).Property(x => x.IsFork).IsModified = true;
                }

                await context.SaveChangesAsync(cancellationToken);
            }

            var allForkedBlocks = await context.BlockEntities
                .Where(b => b.IsFork && b.Ledger == request.Ledger && b.BlockHeight > baseBlock.BlockHeight)
                .ToListAsync(cancellationToken);

            foreach (var forkedBlock in allForkedBlocks.OrderByDescending(p => p.BlockHeight))
            {
                var deleteTransactions = await context.TransactionEntities
                    .Where(t => t.BlockHeight == forkedBlock.BlockHeight && t.BlockHashPrefix == forkedBlock.BlockHashPrefix)
                    .ToListAsync(cancellationToken);
                context.TransactionEntities.RemoveRange(deleteTransactions);
                await context.SaveChangesAsync(cancellationToken);
            }

            var allNonForkedBlocks = await context.BlockEntities
                .Select(p =>
                    new
                    {
                        p.BlockHeight,
                        p.IsFork,
                        p.Ledger,
                        p.BlockHash
                    })
                .Where(b => !b.IsFork && b.Ledger == request.Ledger && b.BlockHeight > baseBlock.BlockHeight)
                .ToListAsync(cancellationToken);
            foreach (var nonForkedBlock in allNonForkedBlocks.OrderBy(p => p.BlockHeight))
            {
                var postgresBlock = await _mediator.Send(new GetPostgresBlockByBlockNoRequest(nonForkedBlock.BlockHeight), cancellationToken);
                if (postgresBlock.IsFailed || !postgresBlock.Value.hash.SequenceEqual(nonForkedBlock.BlockHash))
                {
                    return Result.Fail($"Error retriving expected block {nonForkedBlock.BlockHeight} from {request.Ledger} dbsync database");
                }

                var processBlockResult = await _mediator.Send(new ProcessBlockRequest(postgresBlock.Value, null, null, request.Ledger,request.NewTipBlockHeight,  true));
                if (processBlockResult.IsFailed)
                {
                    return Result.Fail($"Error processing block {nonForkedBlock.BlockHeight} from {request.Ledger} dbsync database for rescan after fork");
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An error occurred while switching branches: {ex.Message}");
        }
    }
}