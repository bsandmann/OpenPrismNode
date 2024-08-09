using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.PostgresModels;

namespace OpenPrismNode.Sync.Commands.ProcessBlock;

public class ProcessBlockRequest : IRequest<Result<Hash?>>
{
    public ProcessBlockRequest(Block block, byte[]? previousHash, LedgerType ledgerType)
    {
        Block = block;
        PreviousBlockHash = previousHash;
        LedgerType = ledgerType;
    }

    public Block Block { get; }
    public byte[]? PreviousBlockHash { get; }
    public LedgerType LedgerType { get; }
}