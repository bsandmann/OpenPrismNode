using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

namespace OpenPrismNode.Sync.Commands.ProcessBlock;

using Core.DbSyncModels;

public class ProcessBlockRequest : IRequest<Result<ProcessBlockResponse>>
{
    public ProcessBlockRequest(Block block, byte[]? previousHash, int? previousBlockHeight, LedgerType ledgerType, int currentBlockTip, bool ignoreCheckForExistingBlock = false)
    {
        Block = block;
        PreviousBlockHash = previousHash;
        PreviousBlockHeight = previousBlockHeight;
        LedgerType = ledgerType;
        IgnoreCheckForExistingBlock = ignoreCheckForExistingBlock;
        CurrentBlockTip = currentBlockTip;
    }

    public Block Block { get; }
    public byte[]? PreviousBlockHash { get; }
    public int? PreviousBlockHeight { get; }
    public LedgerType LedgerType { get; }

    public bool IgnoreCheckForExistingBlock { get; }

    public int CurrentBlockTip { get; }
}