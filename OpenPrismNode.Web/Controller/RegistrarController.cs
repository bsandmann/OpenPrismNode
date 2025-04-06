using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenPrismNode.Web.Models;
using Microsoft.Extensions.Logging;
using FluentResults; // Assuming you use FluentResults for Results

namespace OpenPrismNode.Web.Controller;

using Core;
using Core.Commands;
using Core.Commands.GetDidByOperationHash;
using Core.Commands.GetOperationStatus;
using Core.Commands.GetVerificationMethodSecrets;
using Core.Commands.GetWalletByOperationStatus;
using Core.Commands.Registrar;
using Core.Commands.Registrar.CreateSignedAtalaOperationForCreateDid;
using Core.Commands.Registrar.CreateSignedAtalaOperationForDeactivateDid;
using Core.Commands.Registrar.CreateSignedAtalaOperationForUpdateDid;
using Core.Commands.ResolveDid;
using Core.Commands.ResolveDid.Transform;
using Core.Commands.WriteTransaction;
using Core.Common;
using Core.Entities;
using Core.Models;
using Core.Models.DidDocument;
using Microsoft.Extensions.Options;
using Models;
using Sync;
using Sync.Commands.ParseTransaction;
using Validators;

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
            if (string.IsNullOrWhiteSpace(_appSettings.Value.CardanoWalletApiEndpoint))
            {
               return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
            }
            var ledgertype = LedgerType.CardanoMainnet;
            if (_appSettings.Value.PrismLedger.Name.Equals("preprod", StringComparison.InvariantCultureIgnoreCase))
            {
                ledgertype = LedgerType.CardanoPreprod;
            }

            if (request.JobId is not null)
            {
                // Call the extracted method to handle existing jobs
                return await ProcessExistingJobAsync(request.JobId, request.Options, ledgertype);
            }

            // Ensure Internal Secret Mode defaults if options are missing/null
            var options = request.Options ?? new RegistrarOptions();
            options.StoreSecrets ??= true; // Default to true
            options.ReturnSecrets ??= true; // Default to true

            // Client-managed mode is not supported by this controller implementation
            if (options.ClientSecretMode == true)
            {
                return BadRequest(new ProblemDetails { Title = "Unsupported Operation", Detail = "Client-managed secret mode is not supported." });
            }

            if (request.Method is not null && !request.Method.Equals("prism", StringComparison.InvariantCultureIgnoreCase))
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

            // Validate verification methods and DID document
            var validationResult = RegistrarRequestValidators.ValidateCreateRequest(request);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = validationResult });
            }

            var signedAtalaOperationForCreateDidRequest = new CreateSignedAtalaOperationForCreateDidRequest(
                options,
                request.Secret,
                request.DidDocument
            );

            var signedAtalaOperationResult = await _mediator.Send(signedAtalaOperationForCreateDidRequest);
            if (signedAtalaOperationResult.IsFailed)
            {
                return BadRequest("Failed to create signed Atala operation for DID creation (1) in memory");
            }

            // Actually not required, but avoids that the OPN creates operations that itself cannot parse
            var sanityCheck = await _mediator.Send(new ParseTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, ledgertype, 0, null));
            if (sanityCheck.IsFailed)
            {
                return BadRequest("Failed to create signed Atala operation for DID creation (2) in memory");
            }

            var secretEntities = new List<VerificationMethodSecret>();
            if (signedAtalaOperationResult.Value.PrismDidTemplate.MasterKeyPair is not null)
            {
                secretEntities.Add(new VerificationMethodSecret(
                    keyId: signedAtalaOperationResult.Value.PrismDidTemplate.MasterKeyPair.PublicKey.KeyId,
                    bytes: signedAtalaOperationResult.Value.PrismDidTemplate.MasterKeyPair.PrivateKey.PrivateKey,
                    prismKeyUsage: signedAtalaOperationResult.Value.PrismDidTemplate.MasterKeyPair.PublicKey.KeyUsage.ToString(),
                    curve: signedAtalaOperationResult.Value.PrismDidTemplate.MasterKeyPair.PublicKey.Curve,
                    isRemoveOperation: false,
                    mnemonic: string.Join(" ", signedAtalaOperationResult.Value.PrismDidTemplate.Mnemonic)
                ));
            }

            foreach (var verificationMethodSecrets in signedAtalaOperationResult.Value.PrismDidTemplate.KeyPairs)
            {
                secretEntities.Add(new VerificationMethodSecret(
                    keyId: verificationMethodSecrets.Value.PublicKey.KeyId,
                    bytes: verificationMethodSecrets.Value.PrivateKey.PrivateKey,
                    prismKeyUsage: verificationMethodSecrets.Value.PublicKey.KeyUsage.ToString(),
                    curve: verificationMethodSecrets.Value.PublicKey.Curve,
                    isRemoveOperation: false,
                    mnemonic: null
                ));
            }

            var transactionRequest = new WriteTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, request.Options!.WalletId, secretEntities);

            var transactionResult = await _mediator.Send(transactionRequest);
            if (transactionResult.IsFailed)
            {
                return Ok(new RegistrarResponseDto()
                {
                    DidState = new RegistrarDidState()
                    {
                        State = RegistrarDidState.FailedState,
                        Did = "did:prism:" + sanityCheck.Value.AsCreateDid().didDocument.DidIdentifier,
                        Reason = transactionResult.Errors.FirstOrDefault()?.Message
                    },
                });
            }

            return Ok(new RegistrarResponseDto()
            {
                JobId = PrismEncoding.ByteArrayToHex(transactionResult.Value.OperationStatusId),
                DidState = new RegistrarDidState()
                {
                    State = RegistrarDidState.WaitState,
                    Wait = "Please wait for the transaction to be confirmed on chain",
                },
            });
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
    [HttpPost("update/{did}")] // Using PUT for update
    [ProducesResponseType(typeof(RegistrarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)] // MediatR handler should return specific error for not found
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDid(string did, [FromBody] RegistrarUpdateRequestModel request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_appSettings.Value.CardanoWalletApiEndpoint))
            {
                return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
            }

            var ledgertype = LedgerType.CardanoMainnet;
            if (_appSettings.Value.PrismLedger.Name.Equals("preprod", StringComparison.InvariantCultureIgnoreCase))
            {
                ledgertype = LedgerType.CardanoPreprod;
            }

            if (request.JobId is not null)
            {
                // Call the extracted method to handle existing jobs
                return await ProcessExistingJobAsync(request.JobId, request.Options, ledgertype);
            }

            if (string.IsNullOrWhiteSpace(did))
            {
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "DID must be provided in the URL path." });
            }

            if (!did.Contains("did:prism:"))
            {
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "The DID must be in the format 'did:prism:...'." });
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

            if (string.IsNullOrEmpty(request.Options?.WalletId))
            {
                return BadRequest("WalletId must be provided for updates.");
            }

            if (request.Options?.Network is not null && !request.Options.Network.Equals(_appSettings.Value.PrismLedger.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest($"Invalid network. The specified network does not match the settings of OPN ({_appSettings.Value.PrismLedger.Name})");
            }

            // Validate verification methods and DID document
            var validationResult = RegistrarRequestValidators.ValidateUpdateRequest(request);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return BadRequest(validationResult);
            }

            var signedAtalaOperationForUpdateDidRequest = new CreateSignedAtalaOperationForUpdateDidRequest(
                options,
                request.Secret,
                request.DidDocument!,
                request.DidDocumentOperation!,
                ledgertype,
                did
            );

            var signedAtalaOperationResult = await _mediator.Send(signedAtalaOperationForUpdateDidRequest);
            if (signedAtalaOperationResult.IsFailed)
            {
                return BadRequest($"Failed to create signed Atala operation for DID Update: {signedAtalaOperationResult.Errors.FirstOrDefault()?.Message}");
            }

            // Actually not required, but avoids that the OPN creates operations that itself cannot parse
            var sanityCheck = await _mediator.Send(new ParseTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, ledgertype, 0, new ResolveMode(null, null, null)));
            if (sanityCheck.IsFailed)
            {
                return BadRequest("Failed to create signed Atala operation for DID creation (2) in memory");
            }

            var secretEntities = new List<VerificationMethodSecret>();
            foreach (var verificationMethodSecrets in signedAtalaOperationResult.Value.PrismDidTemplate.KeyPairs)
            {
                secretEntities.Add(new VerificationMethodSecret(
                    keyId: verificationMethodSecrets.Value.PublicKey.KeyId,
                    bytes: verificationMethodSecrets.Value.PrivateKey.PrivateKey,
                    prismKeyUsage: verificationMethodSecrets.Value.PublicKey.KeyUsage.ToString(),
                    curve: verificationMethodSecrets.Value.PublicKey.Curve,
                    isRemoveOperation: false,
                    mnemonic: null
                ));
            }

            var transactionRequest = new WriteTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, request.Options!.WalletId, secretEntities);

            var transactionResult = await _mediator.Send(transactionRequest);
            if (transactionResult.IsFailed)
            {
                return Ok(new RegistrarResponseDto()
                {
                    DidState = new RegistrarDidState()
                    {
                        State = RegistrarDidState.FailedState,
                        Did = "did:prism:" + sanityCheck.Value.AsCreateDid().didDocument.DidIdentifier,
                        Reason = transactionResult.Errors.FirstOrDefault()?.Message
                    },
                });
            }

            return Ok(new RegistrarResponseDto()
            {
                JobId = PrismEncoding.ByteArrayToHex(transactionResult.Value.OperationStatusId),
                DidState = new RegistrarDidState()
                {
                    State = RegistrarDidState.WaitState,
                    Wait = "Please wait for the transaction to be confirmed on chain",
                },
            });

            return Ok();
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
            if (string.IsNullOrWhiteSpace(_appSettings.Value.CardanoWalletApiEndpoint))
            {
                return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
            }

            var ledgertype = LedgerType.CardanoMainnet;
            if (_appSettings.Value.PrismLedger.Name.Equals("preprod", StringComparison.InvariantCultureIgnoreCase))
            {
                ledgertype = LedgerType.CardanoPreprod;
            }

            if (request.JobId is not null)
            {
                // Call the extracted method to handle existing jobs
                return await ProcessExistingJobAsync(request.JobId, request.Options, ledgertype);
            }

            if (string.IsNullOrWhiteSpace(did))
            {
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "DID must be provided in the URL path." });
            }

            // Validate verification methods and DID document
            var validationResult = RegistrarRequestValidators.ValidateDeactivateRequest(request);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return BadRequest(validationResult);
            }

            // Ensure Internal Secret Mode defaults if options are missing/null
            var options = request.Options ?? new RegistrarOptions();

            // Client-managed mode is not supported
            if (options.ClientSecretMode == true)
            {
                return BadRequest(new ProblemDetails { Title = "Unsupported Operation", Detail = "Client-managed secret mode is not supported." });
            }

            var signedAtalaOperationForDeactivateDidRequest = new CreateSignedAtalaOperationForDeactivateDidRequest(
                options,
                ledgertype,
                did
            );

            var signedAtalaOperationResult = await _mediator.Send(signedAtalaOperationForDeactivateDidRequest);
            if (signedAtalaOperationResult.IsFailed)
            {
                return BadRequest($"Failed to create signed Atala operation for DID Update: {signedAtalaOperationResult.Errors.FirstOrDefault()?.Message}");
            }

            // Actually not required, but avoids that the OPN creates operations that itself cannot parse
            var sanityCheck = await _mediator.Send(new ParseTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, ledgertype, 0, new ResolveMode(null, null, null)));
            if (sanityCheck.IsFailed)
            {
                return BadRequest("Failed to create signed Atala operation for DID creation (2) in memory");
            }

            var transactionRequest = new WriteTransactionRequest(signedAtalaOperationResult.Value.SignedAtalaOperation, request.Options!.WalletId, null);

            var transactionResult = await _mediator.Send(transactionRequest);
            if (transactionResult.IsFailed)
            {
                return Ok(new RegistrarResponseDto()
                {
                    DidState = new RegistrarDidState()
                    {
                        State = RegistrarDidState.FailedState,
                        Did = "did:prism:" + sanityCheck.Value.AsCreateDid().didDocument.DidIdentifier,
                        Reason = transactionResult.Errors.FirstOrDefault()?.Message
                    },
                });
            }

            return Ok(new RegistrarResponseDto()
            {
                JobId = PrismEncoding.ByteArrayToHex(transactionResult.Value.OperationStatusId),
                DidState = new RegistrarDidState()
                {
                    State = RegistrarDidState.WaitState,
                    Wait = "Please wait for the transaction to be confirmed on chain",
                },
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DID deactivation for {DID}.", did);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred." });
        }
    }

    private string ToPurposeString(PrismKeyUsage keyUsage)
    {
        switch (keyUsage)
        {
            case PrismKeyUsage.AuthenticationKey: return "authentication"; break;
            case PrismKeyUsage.IssuingKey: return "assertionMethod"; break;
            case PrismKeyUsage.CapabilityDelegationKey: return "capabilityDelegation"; break;
            case PrismKeyUsage.CapabilityInvocationKey: return "capabilityInvocation"; break;
            case PrismKeyUsage.KeyAgreementKey: return "keyAgreement"; break;
            default: return "unsupported"; break;
        }
    }

    /// <summary>
    /// Handles the processing logic when a JobId is provided in the creation request.
    /// Checks the status of the existing job and returns the corresponding DID state.
    /// </summary>
    /// <param name="request">The original registrar creation request.</param>
    /// <param name="ledgertype">The ledger type determined from settings.</param>
    /// <returns>An IActionResult representing the status of the existing job.</returns>
    private async Task<IActionResult> ProcessExistingJobAsync(string jobId, RegistrarOptions? options, LedgerType ledgertype)
    {
        // request.JobId is guaranteed to be non-null here by the caller.
        var byteArrayResult = PrismEncoding.TryHexToByteArray(jobId); // Use null-forgiving operator as check is done before call
        if (byteArrayResult.IsFailed)
        {
            return BadRequest(byteArrayResult.Errors.FirstOrDefault()?.Message);
        }

        var operationStatusResult = await _mediator.Send(new GetOperationStatusRequest(byteArrayResult.Value));
        if (operationStatusResult.IsFailed)
        {
            return BadRequest(operationStatusResult.Errors?.FirstOrDefault()?.Message);
        }

        var didState = RegistrarDidState.FailedState;
        string? did = null;
        string? walletId = null;
        string? mnemonic = null;
        List<RegistrarVerificationMethodPrivateData>? methodPrivateDatas = null;
        DidDocument? didDocument = null;
        DidDocumentMetadata? didDocumentMetadata = null;

        if (operationStatusResult.Value.Status == OperationStatusEnum.ConfirmedAndApplied)
        {
            didState = RegistrarDidState.FinishedState;
            var verifcationMethodSecrets = await _mediator.Send(new GetVerificationMethodSecretsRequest(operationStatusResult.Value.OperationStatusId));
            if (verifcationMethodSecrets.IsFailed)
            {
                // Log the specific error before returning a generic message
                _logger.LogError("Failed to get verification method secrets for operation status ID {OperationStatusId}: {Error}",
                    operationStatusResult.Value.OperationStatusId, verifcationMethodSecrets.Errors?.FirstOrDefault()?.Message);
                return BadRequest("Failed to retrieve necessary secret information to complete the request.");
            }

            var operationHash = operationStatusResult.Value.OperationHash;
            var didIdentifierResult = await _mediator.Send(new GetDidByOperationHashRequest(operationHash, operationStatusResult.Value.OperationType));
            if (didIdentifierResult.IsFailed)
            {
                _logger.LogError("Failed to get DID by operation hash {OperationHash}: {Error}",
                    operationHash, didIdentifierResult.Errors?.FirstOrDefault()?.Message);
                return BadRequest("Failed to retrieve DID identifier associated with the operation.");
            }

            // TODO limit the resolution to the actual operation (consider adding parameters to ResolveDidRequest if needed)
            var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgertype, didIdentifierResult.Value, null, null, null));
            if (resolveResult.IsFailed)
            {
                _logger.LogError("Failed to resolve DID {DID} for JobId {JobId}: {Error}",
                    didIdentifierResult.Value, jobId, resolveResult.Errors?.FirstOrDefault()?.Message);
                return BadRequest("Failed to resolve the created DID document.");
            }

            var walletResult = await _mediator.Send(new GetWalletByOperationStatusIdRequest(operationStatusResult.Value.OperationStatusId));
            if (walletResult.IsFailed)
            {
                // Log the specific error
                _logger.LogError("Failed to get wallet by operation status ID {OperationStatusId} for JobId {JobId}: {Error}",
                    operationStatusResult.Value.OperationStatusId, jobId, walletResult.Errors?.FirstOrDefault()?.Message);
                // Decide if this is a critical failure or if "unknown" walletId is acceptable
                // For now, let's proceed but log a warning. Depending on requirements, might return BadRequest.
                _logger.LogWarning("Could not determine wallet ID for operation status {OperationStatusId}. Setting walletId to 'unknown'.", operationStatusResult.Value.OperationStatusId);
                walletId = "unknown"; // Set explicitly
            }
            else
            {
                walletId = walletResult.Value?.WalletId ?? "unknown"; // Handle null wallet result gracefully
            }

            if (operationStatusResult.Value.OperationType == OperationTypeEnum.CreateDid)
            {
                var returnSecrets = options?.ReturnSecrets ?? true;
                didDocument = TransformToDidDocument.Transform(resolveResult.Value.InternalDidDocument, ledgertype, false, showMasterAndRevocationKeys: false);
                didDocumentMetadata = TransformToDidDocumentMetadata.Transform(resolveResult.Value.InternalDidDocument!, ledgertype, null, false, false);

                did = didDocument.Id;
                // walletId is assigned above

                methodPrivateDatas = new List<RegistrarVerificationMethodPrivateData>();

                // Simplified Master Key Handling
                var masterKey = resolveResult.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyUsage == PrismKeyUsage.MasterKey);
                if (masterKey != null)
                {
                    var masterKeyPrivateKeyJwk = new Dictionary<string, object>
                    {
                        { "crv", PrismParameters.Secp256k1CurveName },
                        { "kty", "EC" },
                        { "x", PrismEncoding.ByteArrayToBase64(masterKey.X!) },
                        { "y", PrismEncoding.ByteArrayToBase64(masterKey.Y!) },
                    };

                    if (returnSecrets)
                    {
                        var masterSecret = verifcationMethodSecrets.Value.FirstOrDefault(p => p.PrismKeyUsage.Equals("masterKey", StringComparison.InvariantCultureIgnoreCase));
                        if (masterSecret != null)
                        {
                            masterKeyPrivateKeyJwk.Add("d", PrismEncoding.ByteArrayToBase64(masterSecret.Bytes));
                            mnemonic = masterSecret.Mnemonic;
                        }
                        else
                        {
                            // Log that the master secret wasn't found, might indicate an issue
                            _logger.LogWarning("Master key secret not found for DID {DID} and JobId {JobId}, although returnSecrets was true.", did, jobId);
                        }
                    }

                    methodPrivateDatas.Add(new RegistrarVerificationMethodPrivateData
                    {
                        Type = "JsonWebKey2020",
                        Controller = did,
                        Purpose = new List<string>() { "masterKey" },
                        PrivateKeyJwk = masterKeyPrivateKeyJwk
                    });
                }
                else
                {
                    // Log that the master key itself wasn't found in the resolved document
                    _logger.LogWarning("Master key not found in resolved DID document for DID {DID} and JobId {JobId}.", did, jobId);
                }


                // Handle other verification methods
                foreach (var verificationMethodKey in resolveResult.Value.InternalDidDocument.PublicKeys.Where(p => p.KeyUsage != PrismKeyUsage.MasterKey))
                {
                    Dictionary<string, object> currentPrivateKeyJwk;

                    if (verificationMethodKey.Curve == PrismParameters.Secp256k1CurveName)
                    {
                        currentPrivateKeyJwk = new Dictionary<string, object>
                        {
                            { "crv", verificationMethodKey.Curve },
                            { "kty", "EC" },
                            { "x", PrismEncoding.ByteArrayToBase64(verificationMethodKey.X!) },
                            { "y", PrismEncoding.ByteArrayToBase64(verificationMethodKey.Y!) },
                        };
                    }
                    else // Assuming Ed25519 or others use OKP format
                    {
                        currentPrivateKeyJwk = new Dictionary<string, object>
                        {
                            { "crv", verificationMethodKey.Curve },
                            { "kty", "OKP" },
                            { "x", PrismEncoding.ByteArrayToBase64(verificationMethodKey.RawBytes!) }
                        };
                    }

                    if (returnSecrets)
                    {
                        var methodSecret = verifcationMethodSecrets.Value.FirstOrDefault(p => p.KeyId.Equals(verificationMethodKey.KeyId, StringComparison.InvariantCultureIgnoreCase));
                        if (methodSecret != null)
                        {
                            currentPrivateKeyJwk.Add("d", PrismEncoding.ByteArrayToBase64(methodSecret.Bytes));
                        }
                        else
                        {
                            _logger.LogWarning("Secret not found for key ID {KeyId} in DID {DID} for JobId {JobId}, although returnSecrets was true.", verificationMethodKey.KeyId, did, jobId);
                        }
                    }

                    methodPrivateDatas.Add(new RegistrarVerificationMethodPrivateData
                    {
                        Type = "JsonWebKey2020",
                        Controller = did,
                        Purpose = new List<string>() { ToPurposeString(verificationMethodKey.KeyUsage) },
                        Id = did + "#" + verificationMethodKey.KeyId,
                        PrivateKeyJwk = currentPrivateKeyJwk
                    });
                }
            }

            if (operationStatusResult.Value.OperationType == OperationTypeEnum.UpdateDid)
            {
                var returnSecrets = options?.ReturnSecrets ?? true;
                didDocument = TransformToDidDocument.Transform(resolveResult.Value.InternalDidDocument, ledgertype, false, showMasterAndRevocationKeys: false);
                didDocumentMetadata = TransformToDidDocumentMetadata.Transform(resolveResult.Value.InternalDidDocument!, ledgertype, null, false, false);

                did = didDocument.Id;
                // walletId is assigned above

                methodPrivateDatas = new List<RegistrarVerificationMethodPrivateData>();

                // Handle other verification methods
                foreach (var verificationMethodKey in resolveResult.Value.InternalDidDocument.PublicKeys.Where(p => p.KeyUsage != PrismKeyUsage.MasterKey))
                {
                    if (verifcationMethodSecrets.Value.Select(p => p.KeyId).Contains(verificationMethodKey.KeyId))
                    {
                        Dictionary<string, object> currentPrivateKeyJwk;

                        if (verificationMethodKey.Curve == PrismParameters.Secp256k1CurveName)
                        {
                            currentPrivateKeyJwk = new Dictionary<string, object>
                            {
                                { "crv", verificationMethodKey.Curve },
                                { "kty", "EC" },
                                { "x", PrismEncoding.ByteArrayToBase64(verificationMethodKey.X!) },
                                { "y", PrismEncoding.ByteArrayToBase64(verificationMethodKey.Y!) },
                            };
                        }
                        else // Assuming Ed25519 or others use OKP format
                        {
                            currentPrivateKeyJwk = new Dictionary<string, object>
                            {
                                { "crv", verificationMethodKey.Curve },
                                { "kty", "OKP" },
                                { "x", PrismEncoding.ByteArrayToBase64(verificationMethodKey.RawBytes!) }
                            };
                        }

                        if (returnSecrets)
                        {
                            var methodSecret = verifcationMethodSecrets.Value.FirstOrDefault(p => p.KeyId.Equals(verificationMethodKey.KeyId, StringComparison.InvariantCultureIgnoreCase));
                            if (methodSecret != null)
                            {
                                currentPrivateKeyJwk.Add("d", PrismEncoding.ByteArrayToBase64(methodSecret.Bytes));
                            }
                            else
                            {
                                _logger.LogWarning("Secret not found for key ID {KeyId} in DID {DID} for JobId {JobId}, although returnSecrets was true.", verificationMethodKey.KeyId, did, jobId);
                            }
                        }

                        methodPrivateDatas.Add(new RegistrarVerificationMethodPrivateData
                        {
                            Type = "JsonWebKey2020",
                            Controller = did,
                            Purpose = new List<string>() { ToPurposeString(verificationMethodKey.KeyUsage) },
                            Id = did + "#" + verificationMethodKey.KeyId,
                            PrivateKeyJwk = currentPrivateKeyJwk
                        });
                    }
                }
            }

            if (operationStatusResult.Value.OperationType == OperationTypeEnum.DeactivateDid)
            {
                didDocument = TransformToDidDocument.Transform(resolveResult.Value.InternalDidDocument, ledgertype, false, showMasterAndRevocationKeys: false);
                didDocumentMetadata = TransformToDidDocumentMetadata.Transform(resolveResult.Value.InternalDidDocument!, ledgertype, null, false, false);
                did = didDocument.Id;
                didDocument = null;
                // walletId is assigned above
            }
        }
        else if (operationStatusResult.Value.Status == OperationStatusEnum.PendingSubmission || operationStatusResult.Value.Status == OperationStatusEnum.AwaitConfirmation)
        {
            didState = RegistrarDidState.WaitState;
        }
        else // Covers Rejected, FailedToSubmit etc.
        {
            didState = RegistrarDidState.FailedState;
            _logger.LogInformation("Operation status for JobId {JobId} is {Status}. Returning FailedState.", jobId, operationStatusResult.Value.Status);
        }

        var responseDto = new RegistrarResponseDto()
        {
            JobId = jobId, // Return the original JobId
            DidState = new RegistrarDidState()
            {
                State = didState,
                Did = did, // Null if not finished or not CreateDid type
                Secret = (methodPrivateDatas != null && methodPrivateDatas.Any()) ? new RegistrarSecret() { VerificationMethod = methodPrivateDatas } : null, // Only include secrets if available
                DidDocument = didDocument, // Null if not finished
                // Include reason if failed? The GetOperationStatus doesn't easily provide one.
                Reason = didState == RegistrarDidState.FailedState ? $"Operation status: {operationStatusResult.Value.Status}" : null,
                Wait = didState == RegistrarDidState.WaitState ? "Operation is pending or awaiting confirmation on the ledger." : null
            },
            DidDocumentMetadata = didDocumentMetadata, // Null if not finished
            DidRegistrationMetadata = new RegistrarDidRegistrationMetadata()
        };

        if (walletId is not null)
        {
            responseDto.DidRegistrationMetadata.Add("walletId", walletId);
        }

        if (mnemonic is not null)
        {
            responseDto.DidRegistrationMetadata.Add("mnemonic", mnemonic);
        }

        // Determine appropriate status code based on final state
        if (didState == RegistrarDidState.FinishedState)
        {
            // Consider 201 Created only if this is the *first* time this JobId resolved to FinishedState.
            // Since we don't track that here, 200 OK is safer for subsequent checks.
            return Ok(responseDto);
        }
        else // WaitState, FailedState
        {
            // 200 OK indicates the request to check the job was accepted and processed, even if the job isn't finished/failed.
            // Could also use 202 Accepted for WaitState, but 200 is common for status checks.
            return Ok(responseDto);
        }
    }
}