namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Core.Commands.DeleteBlock;
using Core.Commands.DeletedOrphanedAddresses;
using Core.Commands.DeleteEmptyEpoch;
using Core.Commands.DeleteEpoch;
using Core.Commands.GetMostRecentBlock;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Commands.DeleteLedger;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class LedgersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackgroundSyncService _backgroundSyncService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<LedgersController> _logger;

    /// <inheritdoc />
    public LedgersController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<LedgersController> logger, BackgroundSyncService backgroundSyncService)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
    }

    /// <summary>
    /// Deletes the complete ledger for a specified network.
    /// </summary>
    /// <remarks>
    /// This operation deletes the ledger data but not the full database.
    /// Any automatic syncing or execution of other tasks is disabled during the deletion process.
    /// Depending on the ledger size, this process may take a few seconds to complete.
    /// The sync service will need to be manually restarted after the deletion if required.
    /// </remarks>
    /// <param name="ledger">The ledger to delete: 'preprod', 'mainnet', or 'inmemory'</param>
    /// <returns>An ActionResult indicating the result of the operation</returns>
    /// <response code="200">The ledger has been successfully deleted</response>
    /// <response code="400">Bad request, due to missing or invalid ledger value</response>
    /// <response code="401">Unauthorized request</response>
    [ApiKeyAdminAuthorization]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpDelete("api/v{version:apiVersion=1.0}/ledgers/{ledger}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> DeleteLedger(string ledger)
    {
        if (string.IsNullOrEmpty(ledger))
        {
            return BadRequest("The ledger must be provided, e.g 'preprod' or 'mainnet'");
        }

        _backgroundSyncService.Lock();
        await _backgroundSyncService.StopAsync(CancellationToken.None);
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");
        _logger.LogInformation($"Deleting {ledger} ledger...");

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            _backgroundSyncService.Unlock();
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        var result = await _mediator.Send(new DeleteLedgerRequest(ledgerType));
        if (result.IsFailed)
        {
            _logger.LogError($"Unable to delete Ledger for prism:{ledger}: {result.Errors.FirstOrDefault()?.Message}");
            ;
            _backgroundSyncService.Unlock();
            return BadRequest(result.Errors.FirstOrDefault());
        }

        var orphanedAddressesDeleteResult = await _mediator.Send(new DeleteOrphanedAddressesRequest(ledgerType));
        if (orphanedAddressesDeleteResult.IsFailed)
        {
            _backgroundSyncService.Unlock();
            return BadRequest($"The orphaned addresses could not be deleted for the ledger {ledger}: {orphanedAddressesDeleteResult.Errors.First().Message}");
        }

        _logger.LogInformation($"Deleting Ledger for prism:{ledger} completed");

        _backgroundSyncService.Unlock();
        return Ok();
    }

    /// <summary>
    /// Deletes a range of blocks from the tip down to a specified block height.
    /// </summary>
    /// <remarks>
    /// This operation deletes blocks from the tip of the chain down to, but not including, the specified block height.
    /// The block at the specified height becomes the new tip of the chain.
    /// Automatic syncing and other tasks are disabled during this operation.
    /// It is not recommended to use this endpoint to delete a large number of blocks, as the process operates on each block individually.
    /// If no block height is specified, only the tip block is deleted.
    /// </remarks>
    /// <param name="blockHeight">The block height to delete up to (not included). If omitted, only the tip block is deleted.</param>
    /// <param name="ledger">The ledger to delete blocks from: 'preprod', 'mainnet', or 'inmemory'.</param>
    /// <returns>An ActionResult indicating the result of the operation</returns>
    /// <response code="200">Blocks have been successfully deleted and the new tip has been set</response>
    /// <response code="400">Bad request due to missing or invalid block height or ledger value</response>
    /// <response code="401">Unauthorized request</response>
    [ApiKeyAdminAuthorization]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpDelete("api/v{version:apiVersion=1.0}/ledgers/{ledger}/block")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> DeleteBlock([FromQuery] int? blockHeight, string ledger)
    {
        if (string.IsNullOrEmpty(ledger))
        {
            return BadRequest("The ledger must be provided, e.g 'preprod' or 'mainnet'");
        }

        _backgroundSyncService.Lock();
        await _backgroundSyncService.StopAsync(CancellationToken.None);
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            _backgroundSyncService.Unlock();
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        if (blockHeight is null)
        {
            _logger.LogInformation($"Deleting tip of {ledger} ledger...");
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            var blockDeleteResult = await _mediator.Send(new DeleteBlockRequest(mostRecentBlock.Value.BlockHeight, mostRecentBlock.Value.BlockHashPrefix, ledgerType));
            if (blockDeleteResult.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block could not be deleted for the ledger {ledger}: {blockDeleteResult.Errors.First().Message}");
            }

            // TODO cache update!

            if (blockDeleteResult.Value.PreviousBlockHeight == 0 && blockDeleteResult.Value.PreviousBlockHashPrefix == 0)
            {
                var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(blockDeleteResult.Value.DeletedBlockWasInEpoch, ledgerType));
                if (epochDeleteResult.IsFailed)
                {
                    _backgroundSyncService.Unlock();
                    return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                }

                _logger.LogInformation($"Deletion completed for the ledger {ledger}. The ledger is now empty.");
                _backgroundSyncService.Unlock();
                return Ok();
            }

            _logger.LogInformation($"Deletion completed for the ledger {ledger}. New tip is now {mostRecentBlock.Value.PreviousBlockHeight}");
            _backgroundSyncService.Unlock();
            return Ok();
        }
        else
        {
            _logger.LogInformation($"Deleting tip of {ledger} ledger until block height {blockHeight} (not included)...");
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            if (mostRecentBlock.Value.BlockHeight < blockHeight)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block height is {mostRecentBlock.Value.BlockHeight}.The provided block height {blockHeight} is greater than the most recent block height.");
            }
            else if (mostRecentBlock.Value.BlockHeight == blockHeight)
            {
                // nothing to delete
                _backgroundSyncService.Unlock();
                Result.Ok();
            }

            int blockHeightAcc = mostRecentBlock.Value.BlockHeight;
            int? blockHashPrefixAcc = mostRecentBlock.Value.BlockHashPrefix;
            int currentEpoch = mostRecentBlock.Value.EpochNumber;

            for (int i = mostRecentBlock.Value.BlockHeight; i > blockHeight; i--)
            {
                var blockDeleteResult = await _mediator.Send(new DeleteBlockRequest(blockHeightAcc, blockHashPrefixAcc, ledgerType));
                if (blockDeleteResult.IsFailed)
                {
                    _backgroundSyncService.Unlock();
                    return BadRequest($"The block {i} could not be deleted for the ledger {ledger}: {blockDeleteResult.Errors.First().Message}");
                }

                if (currentEpoch != blockDeleteResult.Value.DeletedBlockWasInEpoch)
                {
                    var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(currentEpoch, ledgerType));
                    if (epochDeleteResult.IsFailed)
                    {
                        _backgroundSyncService.Unlock();
                        return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                    }

                    var orphanedAddressesDeleteResult = await _mediator.Send(new DeleteOrphanedAddressesRequest(ledgerType));
                    if (orphanedAddressesDeleteResult.IsFailed)
                    {
                        _backgroundSyncService.Unlock();
                        return BadRequest($"The orphaned addresses could not be deleted for the ledger {ledger}: {orphanedAddressesDeleteResult.Errors.First().Message}");
                    }
                }

                if (blockDeleteResult.Value.PreviousBlockHeight == 0 && blockDeleteResult.Value.PreviousBlockHashPrefix == 0)
                {
                    var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(blockDeleteResult.Value.DeletedBlockWasInEpoch, ledgerType));
                    if (epochDeleteResult.IsFailed)
                    {
                        _backgroundSyncService.Unlock();
                        return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                    }

                    var orphanedAddressesDeleteResult = await _mediator.Send(new DeleteOrphanedAddressesRequest(ledgerType));
                    if (orphanedAddressesDeleteResult.IsFailed)
                    {
                        _backgroundSyncService.Unlock();
                        return BadRequest($"The orphaned addresses could not be deleted for the ledger {ledger}: {orphanedAddressesDeleteResult.Errors.First().Message}");
                    }

                    _backgroundSyncService.Unlock();
                    _logger.LogInformation($"Deletion completed for the ledger {ledger}. The ledger is now empty.");
                    return Ok();
                }

                blockHeightAcc = blockDeleteResult.Value.PreviousBlockHeight;
                blockHashPrefixAcc = blockDeleteResult.Value.PreviousBlockHashPrefix;
                currentEpoch = blockDeleteResult.Value.DeletedBlockWasInEpoch;
            }

            // TODO cache update!

            _backgroundSyncService.Unlock();
            _logger.LogInformation($"Deletion completed for the ledger {ledger}. New tip is now {blockHeight}");

            return Ok();
        }
    }

    /// <summary>
    /// Deletes a range of epochs from the tip down to a specified epoch number.
    /// </summary>
    /// <remarks>
    /// This operation deletes epochs from the tip of the chain down to, but not including, the specified epoch number.
    /// The last block of the provided epoch becomes the new tip of the chain.
    /// Automatic syncing and other tasks are disabled during this operation.
    /// If no epoch number is specified, only the most recent epoch is deleted.
    /// This operation also includes cleaning up orphaned addresses after epoch deletion.
    /// </remarks>
    /// <param name="epochNumber">The epoch number to delete up to (not included). If omitted, only the most recent epoch is deleted.</param>
    /// <param name="ledger">The ledger to delete epochs from: 'preprod', 'mainnet', or 'inmemory'.</param>
    /// <returns>An ActionResult indicating the result of the operation</returns>
    /// <response code="200">Epochs have been successfully deleted, orphaned addresses cleaned up, and the new tip has been set</response>
    /// <response code="400">Bad request due to missing or invalid epoch number or ledger value</response>
    /// <response code="401">Unauthorized request</response>
    [ApiKeyAdminAuthorization]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpDelete("api/v{version:apiVersion=1.0}/ledgers/{ledger}/epochs")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> DeleteEpoch([FromQuery]int? epochNumber, string ledger)
    {
        if (string.IsNullOrEmpty(ledger))
        {
            return BadRequest("The ledger must be provided, e.g 'preprod' or 'mainnet'");
        }

        _backgroundSyncService.Lock();
        await _backgroundSyncService.StopAsync(CancellationToken.None);
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            _backgroundSyncService.Unlock();
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        if (epochNumber is null)
        {
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            _logger.LogInformation($"Deleting epoch {mostRecentBlock.Value.EpochNumber} of {ledger} ledger...");

            var epochDeleteResult = await _mediator.Send(new DeleteEpochRequest(mostRecentBlock.Value.EpochNumber, ledgerType));
            if (epochDeleteResult.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent epoch could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
            }

            var orphanedAddressesDeleteResult = await _mediator.Send(new DeleteOrphanedAddressesRequest(ledgerType));
            if (orphanedAddressesDeleteResult.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The orphaned addresses could not be deleted for the ledger {ledger}: {orphanedAddressesDeleteResult.Errors.First().Message}");
            }

            // TODO cache update!

            _backgroundSyncService.Unlock();
            _logger.LogInformation($"Deletion completed for epoch {mostRecentBlock.Value.EpochNumber} of {ledger} ledger");
            return Ok();
        }
        else
        {
            _logger.LogInformation($"Deleting epoch of {ledger} ledger until epoch {epochNumber} (not included)...");
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            if (mostRecentBlock.Value.EpochNumber < epochNumber)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The current epoch is {mostRecentBlock.Value.EpochNumber}.The provided epoch {epochNumber} is greater than the current epoch");
            }
            else if (mostRecentBlock.Value.EpochNumber == epochNumber)
            {
                // nothing to delete
                _backgroundSyncService.Unlock();
                Result.Ok();
            }

            for (int i = mostRecentBlock.Value.EpochNumber; i > epochNumber; i--)
            {
                var epochDeleteResult = await _mediator.Send(new DeleteEpochRequest(i, ledgerType));
                if (epochDeleteResult.IsFailed)
                {
                    _backgroundSyncService.Unlock();
                    return BadRequest($"The epoch {i} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                }

                if (i == 0)
                {
                    _backgroundSyncService.Unlock();
                    _logger.LogInformation($"Deletion completed for the ledger {ledger}. The ledger is now empty.");
                }
            }

            var orphanedAddressesDeleteResult = await _mediator.Send(new DeleteOrphanedAddressesRequest(ledgerType));
            if (orphanedAddressesDeleteResult.IsFailed)
            {
                _backgroundSyncService.Unlock();
                return BadRequest($"The orphaned addresses could not be deleted for the ledger {ledger}: {orphanedAddressesDeleteResult.Errors.First().Message}");
            }

            _backgroundSyncService.Unlock();
            return Ok();
        }

        // TODO cache update!

        _logger.LogInformation($"Deletion completed for the ledger {ledger}. The last epoch in the database is now {epochNumber}");

        return Ok();
    }
}