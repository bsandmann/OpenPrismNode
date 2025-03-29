using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenPrismNode.Web.Models;
using Microsoft.Extensions.Logging;
using FluentResults; // Assuming you use FluentResults for Results

namespace OpenPrismNode.Web.Controller
{
    using Core.Commands;
    using Core.Commands.Registrar;
    using Core.Commands.Registrar.RegistrarCreateDid;
    using Core.Commands.Registrar.RegistrarDeactivateDid;
    using Core.Commands.Registrar.RegistrarUpdateDid;
    using Core.Common;
    using Microsoft.Extensions.Options;
    using Models;

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/registrar")]
    [Produces("application/json")]
    public class RegistrarController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RegistrarController> _logger;
        private readonly IOptions<AppSettings> _appSettings;

        public RegistrarController(IMediator mediator, ILogger<RegistrarController> logger, IOptions<AppSettings> appSettings)
        {
            _mediator = mediator;
            _logger = logger;
            _appSettings = appSettings;
        }

        /// <summary>
        /// Creates a new DID and associated DID document based on the specified method.
        /// Operates in Internal Secret Mode, storing and returning secrets by default.
        /// </summary>
        /// <param name="request">The request body containing creation details.</param>
        /// <returns>The result of the DID creation process.</returns>
        /// <response code="200">Request accepted, potentially ongoing (check didState.state).</response>
        /// <response code="201">DID successfully created (didState.state is 'finished').</response>
        /// <response code="400">Bad request. Invalid input data.</response>
        /// <response code="500">Internal server error during processing.</response>
        [HttpPost("create")] // Changed route slightly for clarity
        [ProducesResponseType(typeof(RegistrarResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RegistrarResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDid([FromBody] RegistrarCreateRequestModel request)
        {
            try
            {
                // Ensure Internal Secret Mode defaults if options are missing/null
                var options = request.Options ?? new RegistrarOptions();
                options.StoreSecrets ??= true; // Default to true
                options.ReturnSecrets ??= true; // Default to true

                // Client-managed mode is not supported by this controller implementation
                if (options.ClientSecretMode == true)
                {
                    return BadRequest(new ProblemDetails { Title = "Unsupported Operation", Detail = "Client-managed secret mode is not supported." });
                }

                if (!request.Method.Equals("prism", StringComparison.InvariantCultureIgnoreCase))
                {
                    return BadRequest("Invalid method. Only 'prism' is supported.");
                }

                if (!string.IsNullOrEmpty(request.Did))
                {
                    return BadRequest("DID should not be provided for creation for did:prism operations. It will be generated.");
                }

                if (string.IsNullOrEmpty(request.Options?.WalletId))
                {
                    return BadRequest("WalletId must be provided for creation.");
                }

                if (request.Options?.Network is not null && !request.Options.Network.Equals(_appSettings.Value.PrismLedger.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return BadRequest($"Invalid network. The specified network does not match the settings of OPN ({_appSettings.Value.PrismLedger.Name})");
                }

                var registrarCreateDidRequest = new RegistrarCreateDidRequest(
                    options,
                    request.Secret,
                    request.DidDocument
                );

                Result<RegistrarResponseDto> result = await _mediator.Send(registrarCreateDidRequest);

                if (result.IsFailed)
                {
                    _logger.LogWarning("DID Creation failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
                    // Distinguish between user errors (400) and system errors (500) if possible from Result reasons
                    // For simplicity now, return 400 for any failure from MediatR
                    return BadRequest(new ProblemDetails { Title = "DID Creation Failed", Detail = string.Join("; ", result.Errors.Select(e => e.Message)) });
                }

                var response = result.Value;

                if (response.DidState?.State == RegistrarDidState.FinishedState)
                {
                    // Return 201 Created if finished successfully
                    // Construct the URL to the newly created resource if applicable/possible
                    // string locationUrl = Url.Action("ResolveDid", "Resolve", new { did = response.DidState.Did }, Request.Scheme) ?? string.Empty;
                    // return Created(locationUrl, response);
                    return StatusCode(StatusCodes.Status201Created, response); // Simplier 201 without location
                }
                else
                {
                    // Return 200 OK if still pending (action, wait) or potentially even if failed within the DTO state
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during DID creation.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates the DID document associated with the given DID.
        /// Operates in Internal Secret Mode.
        /// </summary>
        /// <param name="did">The DID to update.</param>
        /// <param name="request">The request body containing update details.</param>
        /// <returns>The result of the DID update process.</returns>
        /// <response code="200">Request successful, update may be finished or ongoing (check didState.state).</response>
        /// <response code="400">Bad request. Invalid input data or DID format.</response>
        /// <response code="404">Not Found. The specified DID does not exist.</response>
        /// <response code="500">Internal server error during processing.</response>
        [HttpPut("update/{did}")] // Using PUT for update
        [ProducesResponseType(typeof(RegistrarResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)] // MediatR handler should return specific error for not found
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDid(string did, [FromBody] RegistrarUpdateRequestModel request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(did))
                {
                    return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "DID must be provided in the URL path." });
                }

                // Ensure Internal Secret Mode defaults if options are missing/null
                var options = request.Options ?? new RegistrarOptions();
                options.StoreSecrets ??= true; // Default to true
                options.ReturnSecrets ??= true; // Default to true

                // Client-managed mode is not supported
                if (options.ClientSecretMode == true)
                {
                    return BadRequest(new ProblemDetails { Title = "Unsupported Operation", Detail = "Client-managed secret mode is not supported." });
                }

                var registrarUpdateDidRequest = new RegistrarUpdateDidRequest(
                    did,
                    options,
                    request.Secret,
                    request.DidDocumentOperation ?? new List<string> { RegistrarDidDocumentOperation.SetDidDocument }, // Default to setDidDocument
                    request.DidDocument
                );

                Result<RegistrarResponseDto> result = await _mediator.Send(registrarUpdateDidRequest);

                if (result.IsFailed)
                {
                    _logger.LogWarning("DID Update failed for {DID}: {Errors}", did, string.Join(", ", result.Errors.Select(e => e.Message)));
                    // Add specific check for 'Not Found' errors if your MediatR handler provides them
                    // bool isNotFound = result.Errors.Any(e => e.Metadata.ContainsKey("ErrorCode") && e.Metadata["ErrorCode"].ToString() == "NotFound");
                    // if (isNotFound) return NotFound(...);
                    return BadRequest(new ProblemDetails { Title = "DID Update Failed", Detail = string.Join("; ", result.Errors.Select(e => e.Message)) });
                }

                return Ok(result.Value); // Always 200 OK for update success/pending
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during DID update for {DID}.", did);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deactivates the specified DID.
        /// Operates in Internal Secret Mode.
        /// </summary>
        /// <param name="did">The DID to deactivate.</param>
        /// <param name="request">The request body containing deactivation options and secrets.</param>
        /// <returns>The result of the DID deactivation process.</returns>
        /// <response code="200">Request successful, deactivation may be finished or ongoing (check didState.state).</response>
        /// <response code="400">Bad request. Invalid input data or DID format.</response>
        /// <response code="404">Not Found. The specified DID does not exist.</response>
        /// <response code="500">Internal server error during processing.</response>
        [HttpPost("deactivate/{did}")] // Using POST for Deactivate as DELETE often discourages bodies
        [ProducesResponseType(typeof(RegistrarResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)] // MediatR handler should return specific error for not found
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateDid(string did, [FromBody] RegistrarDeactivateRequestModel request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(did))
                {
                    return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "DID must be provided in the URL path." });
                }

                // Ensure Internal Secret Mode defaults if options are missing/null
                var options = request.Options ?? new RegistrarOptions();
                options.StoreSecrets ??= true; // Default to true
                options.ReturnSecrets ??= true; // Default to true

                // Client-managed mode is not supported
                if (options.ClientSecretMode == true)
                {
                    return BadRequest(new ProblemDetails { Title = "Unsupported Operation", Detail = "Client-managed secret mode is not supported." });
                }

                var registrarDeactivateDidRequest = new RegistrarDeactivateDidRequest(
                    did,
                    options,
                    request.Secret
                );

                Result<RegistrarResponseDto> result = await _mediator.Send(registrarDeactivateDidRequest);

                if (result.IsFailed)
                {
                    _logger.LogWarning("DID Deactivation failed for {DID}: {Errors}", did, string.Join(", ", result.Errors.Select(e => e.Message)));
                    // Add specific check for 'Not Found' errors if your MediatR handler provides them
                    // bool isNotFound = result.Errors.Any(e => e.Metadata.ContainsKey("ErrorCode") && e.Metadata["ErrorCode"].ToString() == "NotFound");
                    // if (isNotFound) return NotFound(...);
                    return BadRequest(new ProblemDetails { Title = "DID Deactivation Failed", Detail = string.Join("; ", result.Errors.Select(e => e.Message)) });
                }

                return Ok(result.Value); // Always 200 OK for deactivate success/pending
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during DID deactivation for {DID}.", did);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred." });
            }
        }
    }
}