namespace OpenPrismNode.Web.Controller;

using Core.Commands.DeleteBlock;
using Core.Commands.DeletedOrphanedAddresses;
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
public class DeleteController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackgroundSyncService _backgroundSyncService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<DeleteController> _logger;

    /// <inheritdoc />
    public DeleteController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<DeleteController> logger, BackgroundSyncService backgroundSyncService)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
    }

    /// <summary>
    /// Deletes the complete Ledger (but not the full database) 
    /// Any automatic syncing or the execution of other tasks is diabled in the meantime
    /// </summary>
    /// <returns></returns>
    [HttpDelete("api/delete/ledger")]
    public async Task<ActionResult> DeleteLedger([FromQuery] string ledger)
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

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
    /// Deletes a range of block beginninning from the tip down to a specific block height (not included in the delete)
    /// That means the block with the provided blockheight will be the new tip of the chain
    /// Any automatic syncing or the execution of other tasks is diabled in the meantime
    /// </summary>
    /// <returns></returns>
    [HttpDelete("api/delete/block")]
    public async Task<ActionResult> DeleteBlock([FromQuery] int? blockHeight, string ledger)
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

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
    /// Deletes a range of epoch beginninning from the tip down to a specific epoch-number (not included in the delete)
    /// That means the last block of the provided epoch will be the new tip of the chain
    /// Any automatic syncing or the execution of other tasks is diabled in the meantime
    /// </summary>
    /// <returns></returns>
    [HttpDelete("api/delete/epoch")]
    public async Task<ActionResult> DeleteEpoch([FromQuery] int? epochNumber, string ledger)
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

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