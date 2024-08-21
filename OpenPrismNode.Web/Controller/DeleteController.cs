namespace OpenPrismNode.Web.Controller;

using Core.Commands.DeleteBlock;
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
    public async Task<ActionResult> Delete([FromQuery] string ledger)
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

        await _backgroundSyncService.StopAsync(CancellationToken.None);
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");
        _logger.LogInformation($"Deleting {ledger} ledger...");

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        var result = await _mediator.Send(new DeleteLedgerRequest(ledgerType));

        if (result.IsFailed)
        {
            _logger.LogError($"Unable to delete Ledger for prism:{ledger}");
            _logger.LogError($"{result.Errors.FirstOrDefault()?.Message}");
            return BadRequest(result.Errors.FirstOrDefault());
        }

        _logger.LogInformation($"Deleting Ledger for prism:{ledger} completed");

        return Ok();
    }

    /// <summary>
    /// Deletes a range of block beginninning from the tip down to a specific block height (not included in the delete)
    /// That means the block with the provided blockheight will be the new tip of the chain
    /// Any automatic syncing or the execution of other tasks is diabled in the meantime
    /// </summary>
    /// <returns></returns>
    [HttpDelete("api/delete/block")]
    public async Task<ActionResult> Delete([FromQuery] int? blockHeight, string ledger)
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

        await _backgroundSyncService.StopAsync(CancellationToken.None);
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        if (blockHeight is null)
        {
            _logger.LogInformation($"Deleting tip of {ledger} ledger...");
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            var blockDeleteResult = await _mediator.Send(new DeleteBlockRequest(mostRecentBlock.Value.BlockHeight, mostRecentBlock.Value.BlockHashPrefix, ledgerType));
            if (blockDeleteResult.IsFailed)
            {
                return BadRequest($"The most recent block could not be deleted for the ledger {ledger}: {blockDeleteResult.Errors.First().Message}");
            }

            // TODO cache update!

            if (blockDeleteResult.Value.PreviousBlockHeight == 0 && blockDeleteResult.Value.PreviousBlockHashPrefix == 0)
            {
                var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(blockDeleteResult.Value.DeletedBlockWasInEpoch, ledgerType));
                if (epochDeleteResult.IsFailed)
                {
                    return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                }

                _logger.LogInformation($"Deletion completed for the ledger {ledger}. The ledger is now empty.");
                return Ok();
            }

            _logger.LogInformation($"Deletion completed for the ledger {ledger}. New tip is now {mostRecentBlock.Value.PreviousBlockHeight}");
            return Ok();
        }
        else
        {
            _logger.LogInformation($"Deleting tip of {ledger} ledger until block height {blockHeight} (not included)...");
            var mostRecentBlock = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
            if (mostRecentBlock.IsFailed)
            {
                return BadRequest($"The most recent block could not be found for the ledger {ledger}: {mostRecentBlock.Errors.First().Message}");
            }

            if (mostRecentBlock.Value.BlockHeight < blockHeight)
            {
                return BadRequest($"The most recent block height is {mostRecentBlock.Value.BlockHeight}.The provided block height {blockHeight} is greater than the most recent block height.");
            }
            else if (mostRecentBlock.Value.BlockHeight == blockHeight)
            {
                // nothing to delete
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
                    return BadRequest($"The block {i} could not be deleted for the ledger {ledger}: {blockDeleteResult.Errors.First().Message}");
                }

                if (currentEpoch != blockDeleteResult.Value.DeletedBlockWasInEpoch)
                {
                    var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(currentEpoch, ledgerType));
                    if (epochDeleteResult.IsFailed)
                    {
                        return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                    }
                }

                if (blockDeleteResult.Value.PreviousBlockHeight == 0 && blockDeleteResult.Value.PreviousBlockHashPrefix == 0)
                {
                    var epochDeleteResult = await _mediator.Send(new DeleteEmptyEpochRequest(blockDeleteResult.Value.DeletedBlockWasInEpoch, ledgerType));
                    if (epochDeleteResult.IsFailed)
                    {
                        return BadRequest($"The epoch {blockDeleteResult.Value.DeletedBlockWasInEpoch} could not be deleted for the ledger {ledger}: {epochDeleteResult.Errors.First().Message}");
                    }

                    _logger.LogInformation($"Deletion completed for the ledger {ledger}. The ledger is now empty.");
                    return Ok();
                }

                blockHeightAcc = blockDeleteResult.Value.PreviousBlockHeight;
                blockHashPrefixAcc = blockDeleteResult.Value.PreviousBlockHashPrefix;
                currentEpoch = blockDeleteResult.Value.DeletedBlockWasInEpoch;
            }

            // TODO cache update!

            _logger.LogInformation($"Deletion completed for the ledger {ledger}. New tip is now {blockHeight}");

            return Ok();
        }
    }
}