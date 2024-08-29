using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Commands.SwitchBranch;
using OpenPrismNode.Core.Entities;

public class SwitchBranchHandler : IRequestHandler<SwitchBranchRequest, Result>
{
    private readonly DataContext _context;

    public SwitchBranchHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(SwitchBranchRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            // Find the base block
            var baseBlock = await _context.BlockEntities
                .FirstOrDefaultAsync(b => b.BlockHeight == request.BaseBlockHeight && b.BlockHashPrefix == request.BaseBlockPrefix && b.Ledger == request.Ledger, cancellationToken);

            if (baseBlock == null)
            {
                return Result.Fail($"Base block not found at height {request.BaseBlockHeight} with prefix {request.BaseBlockPrefix}");
            }

            // Find the new tip block
            var newTipBlock = await _context.BlockEntities
                .FirstOrDefaultAsync(b => b.BlockHeight == request.NewTipBlockHeight && b.BlockHashPrefix == request.NewTipBlockPrefix && b.Ledger == request.Ledger, cancellationToken);

            if (newTipBlock == null)
            {
                return Result.Fail($"New tip block not found at height {request.NewTipBlockHeight} with prefix {request.NewTipBlockPrefix}");
            }

            // Get all blocks after the base block
            var blocksAfterBase = await _context.BlockEntities
                .Where(b => b.BlockHeight > baseBlock.BlockHeight && b.Ledger == request.Ledger)
                .ToListAsync(cancellationToken);

            // Find the path from new tip to base block
            var newMainChain = new HashSet<(int Height, int Prefix)>();
            var currentBlock = newTipBlock;
            while (currentBlock.BlockHeight > baseBlock.BlockHeight)
            {
                newMainChain.Add((currentBlock.BlockHeight, currentBlock.BlockHashPrefix));
                currentBlock = await _context.BlockEntities
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
                _context.BlockEntities.AttachRange(blocksToUpdate);
                foreach (var block in blocksToUpdate)
                {
                    _context.Entry(block).Property(x => x.IsFork).IsModified = true;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An error occurred while switching branches: {ex.Message}");
        }
    }
}