namespace OpenPrismNode.Core.Commands.DeleteBlock;

using FluentResults;
using MediatR;
using Models;

public class DeleteBlockResponse
{
    public DeleteBlockResponse(int previousBlockHeight, int previousBlockHashPrefix, int deletedBlockWasInEpoch)
    {
        PreviousBlockHeight = previousBlockHeight;
        PreviousBlockHashPrefix = previousBlockHashPrefix;
        DeletedBlockWasInEpoch = deletedBlockWasInEpoch;
    }

    public int PreviousBlockHeight { get; }
    public int? PreviousBlockHashPrefix { get; }
    
    public int DeletedBlockWasInEpoch { get; set; }
}
