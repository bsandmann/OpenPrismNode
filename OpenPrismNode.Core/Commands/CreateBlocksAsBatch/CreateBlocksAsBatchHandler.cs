using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateBlocksAsBatch
{
    using Models;

    public class CreateBlocksAsBatchHandler : IRequestHandler<CreateBlocksAsBatchRequest, Result<Hash>>
    {
        private readonly DataContext _context;

        public CreateBlocksAsBatchHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<Result<Hash>> Handle(CreateBlocksAsBatchRequest request, CancellationToken cancellationToken)
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            // Check if the highest block in the database is exactly one less than the first block we're adding
            var highestExistingBlock = await _context.BlockEntities
                .Include(p => p.EpochEntity)
                .Where(b => b.IsFork == false && b.EpochEntity.Ledger == request.ledger)
                .OrderByDescending(b => b.BlockHeight)
                .FirstOrDefaultAsync(cancellationToken);

            if (highestExistingBlock == null || highestExistingBlock.BlockHeight != request.Blocks.First().block_no - 1)
            {
                return Result.Fail("The existing blocks in the database do not align with the new batch.");
            }

            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var newBlocks = new List<BlockEntity>();

            for (int i = 0; i < request.Blocks.Count; i++)
            {
                var block = request.Blocks[i];
                var previousBlock = i == 0 ? highestExistingBlock : newBlocks[i - 1];

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

            await _context.BlockEntities.AddRangeAsync(newBlocks, cancellationToken);

            // Update ledger LastSynced time
            await _context.LedgerEntities
                .Where(n => n.Ledger == request.ledger)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.LastSynced, dateTimeNow), cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Ok(Hash.CreateFrom(request.Blocks.Last().hash));
        }
    }
}