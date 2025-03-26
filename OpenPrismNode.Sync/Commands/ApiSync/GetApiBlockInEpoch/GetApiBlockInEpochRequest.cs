namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockInEpoch;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve a block by epoch number and slot number from the Blockfrost API.
/// </summary>
public class GetApiBlockInEpochRequest : IRequest<Result<Block?>>
{
    /// <summary>
    /// The epoch number to retrieve.
    /// </summary>
    public int EpochNumber { get; }

    /// <summary>
    /// The slot number within the epoch to retrieve.
    /// </summary>
    public int SlotNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockInEpochRequest"/> class.
    /// </summary>
    /// <param name="epochNumber">The epoch number</param>
    /// <param name="slotNumber">The slot number in the given epoch</param>
    public GetApiBlockInEpochRequest(int epochNumber, int slotNumber)
    {
        EpochNumber = epochNumber;
        SlotNumber = slotNumber;
    }
}