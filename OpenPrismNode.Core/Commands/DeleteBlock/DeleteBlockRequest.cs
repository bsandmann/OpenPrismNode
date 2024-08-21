namespace OpenPrismNode.Core.Commands.DeleteBlock;

using FluentResults;
using MediatR;
using Models;

public class DeleteBlockRequest : IRequest<Result<DeleteBlockResponse>>
{
    /// <summary>
    /// Request Constructor
    /// </summary>
    /// <param name="blockHeight"></param>
    /// <param name="blockHashPrefix"></param>
    /// <param name="ledger"></param>
    public DeleteBlockRequest(int blockHeight, int? blockHashPrefix, LedgerType ledger)
    {
        BlockHeight = blockHeight;
        BlockHashPrefix = blockHashPrefix;
        Ledger = ledger;
    }

    public LedgerType Ledger { get; }
    public int BlockHeight { get; }
    public int? BlockHashPrefix { get; }
}