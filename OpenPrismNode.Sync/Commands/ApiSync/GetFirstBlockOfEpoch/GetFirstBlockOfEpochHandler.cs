namespace OpenPrismNode.Sync.Commands.ApiSync.GetFirstBlockOfEpoch;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockInEpoch;

/// <summary>
/// Handler that finds the first block for a given epoch by querying each slot until a valid block is found.
/// </summary>
public class GetFirstBlockOfEpochHandler : IRequestHandler<GetFirstBlockOfEpochRequest, Result<Block>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetFirstBlockOfEpochHandler> _logger;

    // For safety, define a maximum number of slots to check within an epoch.
    // Adjust this limit according to your chain's typical epoch size.
    private const int MAX_SLOTS_PER_EPOCH = 1_000_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFirstBlockOfEpochHandler"/> class.
    /// </summary>
    /// <param name="mediator">The mediator used to call the GetApiBlockInEpochRequest</param>
    /// <param name="logger">The logger</param>
    public GetFirstBlockOfEpochHandler(IMediator mediator, ILogger<GetFirstBlockOfEpochHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the logic of retrieving the first block in a given epoch.
    /// </summary>
    /// <param name="request">The request containing the epoch number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first block found in the epoch or an error if none is found or unexpected errors occur.</returns>
    public async Task<Result<Block>> Handle(GetFirstBlockOfEpochRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching for the first block in epoch {EpochNumber}", request.EpochNumber);

        for (int slot = 0; slot < MAX_SLOTS_PER_EPOCH; slot++)
        {
            // Request the block for epoch {EpochNumber}, slot {slot}
            var blockInEpochResult = await _mediator.Send(
                new GetApiBlockInEpochRequest(request.EpochNumber, slot),
                cancellationToken);

            // If there's a failure, and it's not a 404 scenario, we stop and return the error.
            // (Remember, 404 is handled as a success-with-null in our updated GetApiBlockInEpoch)
            if (blockInEpochResult.IsFailed)
            {
                _logger.LogWarning(
                    "An error occurred while fetching block for epoch {Epoch}, slot {Slot}: {Error}",
                    request.EpochNumber,
                    slot,
                    blockInEpochResult.Errors[0].Message);

                // Return the error immediately
                return Result.Fail<Block>(blockInEpochResult.Errors);
            }

            // If IsSuccess, check whether a block was found
            var block = blockInEpochResult.Value;
            if (block != null)
            {
                _logger.LogInformation(
                    "Found the first block in epoch {Epoch} at slot {Slot}",
                    request.EpochNumber,
                    slot);

                return Result.Ok(block);
            }
            else
            {
                // null block => 404 from the API => keep searching
            }
        }

        _logger.LogWarning("No block was found in epoch {EpochNumber} within {MaxSlots} slots.",
            request.EpochNumber,
            MAX_SLOTS_PER_EPOCH);

        return Result.Fail<Block>($"Could not find any block in epoch {request.EpochNumber} up to slot {MAX_SLOTS_PER_EPOCH - 1}.");
    }
}
