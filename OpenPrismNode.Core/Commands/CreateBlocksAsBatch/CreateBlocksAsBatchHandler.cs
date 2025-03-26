using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateBlocksAsBatch
{
    using FluentResults;
    using Microsoft.Extensions.DependencyInjection;
    using Models;

    public class CreateBlocksAsBatchHandler : IRequestHandler<CreateBlocksAsBatchRequest, Result<Hash>>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CreateBlocksAsBatchHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Result<Hash>> Handle(CreateBlocksAsBatchRequest request, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var distinctBlockHashes = request.Blocks.Select(b => b.hash).Distinct().ToList();
            if (distinctBlockHashes.Count != request.Blocks.Count)
            {
               throw new Exception("Duplicate block hashes found in the batch.");
            }

            // Check if the highest block in the database is exactly one less than the first block we're adding
            var highestExistingNonForkBlock = await context.BlockEntities
                .Include(p => p.EpochEntity)
                .Where(b => b.IsFork == false && b.EpochEntity.Ledger == request.ledger)
                .OrderByDescending(b => b.BlockHeight)
                .FirstOrDefaultAsync(cancellationToken);

            var highestExistingForkBlock = await context.BlockEntities
                .Include(p => p.EpochEntity)
                .Where(b => b.IsFork == true && b.EpochEntity.Ledger == request.ledger)
                .OrderByDescending(b => b.BlockHeight)
                .FirstOrDefaultAsync(cancellationToken);

            if (highestExistingNonForkBlock == null || highestExistingNonForkBlock.BlockHeight != request.Blocks.First().block_no - 1)
            {
                return Result.Fail("The existing blocks in the database do not align with the new batch.");
            }

            if (highestExistingForkBlock is not null && highestExistingForkBlock.BlockHeight >= highestExistingNonForkBlock.BlockHeight)
            {
                // Special case, in which we restarted the sync process on a fork    
                // We remove all the blocks that are part of the fork to avoid conflicts
                var forkedBlocks = await context.BlockEntities
                    .Where(b => b.BlockHeight >= highestExistingForkBlock.BlockHeight && b.EpochEntity.Ledger == request.ledger && b.IsFork)
                    .ToListAsync(cancellationToken: cancellationToken);
                context.RemoveRange(forkedBlocks);
                await context.SaveChangesAsync(cancellationToken);
            }

            // context.ChangeTracker.Clear();
            // context.ChangeTracker.AutoDetectChangesEnabled = false;

            var distinctBlockHashes2 = request.Blocks.Select(b => b.block_no).Distinct().ToList();
            if (distinctBlockHashes2.Count != request.Blocks.Count)
            {
                // duplicate
            }

            var fff = request.Blocks.Where(p => BlockEntity.CalculateBlockHashPrefix(p.hash) == null).ToList();

            var distinctBlockHashes3 = request.Blocks.Select(b =>BlockEntity.CalculateBlockHashPrefix(b.hash)).Distinct().ToList();
            if (distinctBlockHashes3.Count != request.Blocks.Count)
            {
                // duplicate
            }



            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var newBlocks = new List<BlockEntity>();

            for (int i = 0; i < request.Blocks.Count; i++)
            {
                var block = request.Blocks[i];
                var previousBlock = i == 0 ? highestExistingNonForkBlock : newBlocks[i - 1];

                var blockEntity = new BlockEntity
                {
                    BlockHeight = block.block_no,
                    BlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(block.hash) ?? 0,
                    BlockHash = block.hash,
                    EpochNumber = block.epoch_no,
                    TimeUtc = DateTime.SpecifyKind(block.time, DateTimeKind.Unspecified),
                    TxCount = block.tx_count,
                    LastParsedOnUtc = dateTimeNow,
                    PreviousBlockHeight = previousBlock.BlockHeight,
                    PreviousBlockHashPrefix = previousBlock.BlockHashPrefix,
                    IsFork = false, // Assuming IsFork is not part of the Block class, defaulting to false
                    Ledger = request.ledger
                };

                newBlocks.Add(blockEntity);
            }

            await context.BlockEntities.AddRangeAsync(newBlocks, cancellationToken);

            // Update ledger LastSynced time
            await context.LedgerEntities
                .Where(n => n.Ledger == request.ledger)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.LastSynced, dateTimeNow), cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            return Result.Ok(Hash.CreateFrom(request.Blocks.Last().hash));
        }
    }
}