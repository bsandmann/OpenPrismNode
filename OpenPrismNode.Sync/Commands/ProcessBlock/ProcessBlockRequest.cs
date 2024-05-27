using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.PostgresModels;

namespace OpenPrismNode.Sync.Commands.ProcessBlock;

public class ProcessBlockRequest : IRequest<Result<Hash?>>
{
    public ProcessBlockRequest(Block block, Hash? previousHash)
    {
        Block = block;
        PreviousBlockHash = previousHash;
    }

    public Block Block { get; }
    public Hash? PreviousBlockHash { get; }
}