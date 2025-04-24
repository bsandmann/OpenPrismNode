namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForDeactivateDid
{
    using Common;
    using CreateSignedAtalaOperationForUpdateDid;
    using FluentResults;
    using GetOperationStatusByOperationHash;
    using GetVerificationMethodSecrets;
    using Google.Protobuf;
    using MediatR;
    using Models;
    using OpenPrismNode.Core.Crypto;
    using OpenPrismNode.Core.Services.Did;
    using ResolveDid;

    /// <summary>
    /// MediatR command to handle the creation of a DID via the registrar.
    /// </summary>
    public class CreateSignedAtalaOperationForDeactivateDidHandler : IRequestHandler<CreateSignedAtalaOperationForDeactivateDidRequest, Result<CreateSignedAtalaOperationForDeactivateDidResponse>>
    {
        private IKeyGenerationService _keyGenerationService;
        private ISha256Service _sha256Service;
        private ICryptoService _cryptoService;
        private IMediator _mediator;

        /// <summary>
        /// Creates a new instance of the RegistrarCreateDidCommand.
        /// </summary>
        public CreateSignedAtalaOperationForDeactivateDidHandler(
            IMediator mediator,
            IKeyGenerationService keyGenerationService,
            ISha256Service sha256Service,
            ICryptoService cryptoService)
        {
            _mediator = mediator;
            _keyGenerationService = keyGenerationService;
            _sha256Service = sha256Service;
            _cryptoService = cryptoService;
        }

        public async Task<Result<CreateSignedAtalaOperationForDeactivateDidResponse>> Handle(CreateSignedAtalaOperationForDeactivateDidRequest request, CancellationToken cancellationToken)
        {
            var didIdentifierArray = request.Did.Split(":");
            if (didIdentifierArray.Length != 3)
            {
                return Result.Fail("Invalid DID format");
            }

            var didIdentifier = didIdentifierArray[2];
            var resolveDidResult = await _mediator.Send(new ResolveDidRequest(request.LedgerType, didIdentifier, null, null, null), cancellationToken);
            if (resolveDidResult.IsFailed)
            {
                return resolveDidResult.ToResult();
            }

            if (resolveDidResult.Value.InternalDidDocument.Deactivated)
            {
                return Result.Fail("DID is already deactivated");
            }

            var masterKeyPair = resolveDidResult.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyUsage == PrismKeyUsage.MasterKey);
            if (masterKeyPair is null)
            {
                return Result.Fail("Public Master key pair not found");
            }
            var lastOperationHash = resolveDidResult.Value.LastOperationHash;

            var operationStatus = await _mediator.Send(new GetOperationStatusByOperationHashRequest(lastOperationHash.Value), cancellationToken);
            if (operationStatus.IsFailed)
            {
                return Result.Fail("The operation status is not found. The requested did was not created by this node.");
            }

            if (operationStatus.Value.OperationType == OperationTypeEnum.UpdateDid)
            {
                // We need the earilier create DID opeation for the masterkeys
                var initialDidOperationHash = PrismEncoding.HexToByteArray(didIdentifier);
                operationStatus = await _mediator.Send(new GetOperationStatusByOperationHashRequest(initialDidOperationHash), cancellationToken);
                if (operationStatus.IsFailed)
                {
                    return Result.Fail("The operation status is not found. The requested did was not created by this node.");
                }
            }
            else if (operationStatus.Value.OperationType == OperationTypeEnum.DeactivateDid)
            {
                return Result.Fail("The requested did is deactivated. No updates are possible.");
            }
            else if (operationStatus.Value.OperationType == OperationTypeEnum.ProtocolVersionUpdate || operationStatus.Value.OperationType == OperationTypeEnum.Unknown)
            {
                return Result.Fail("Invalid prism operation type.");
            }

            var vericationMethodSecrets = await _mediator.Send(new GetVerificationMethodSecretsRequest(operationStatus.Value.OperationStatusId), cancellationToken);
            if (vericationMethodSecrets.IsFailed)
            {
                return Result.Fail("The verification method secrets are not found. The requested did was not created by this node, or the secrets were not created.");
            }

            var masterKeySecrets = vericationMethodSecrets.Value.FirstOrDefault(p => p.PrismKeyUsage == PrismKeyUsage.MasterKey.ToString());
            if (masterKeySecrets is null)
            {
                return Result.Fail("The master key secrets are not found.");
            }

            if (masterKeySecrets.Mnemonic is null)
            {
                return Result.Fail("The master key seed is not found.");
            }


            var atalaOperation =
                new AtalaOperation
                {
                    DeactivateDid = new DeactivateDIDOperation()
                    {
                        Id = didIdentifier,
                        PreviousOperationHash = PrismEncoding.ByteArrayToByteString(lastOperationHash.Value),
                    }
                };

            var masterKeySecret = PrismEncoding.Base64ToByteArray(request.Options.MasterKeySecretString!);
            if (!masterKeySecret.SequenceEqual(masterKeySecrets.Bytes))
            {
                return Result.Fail("The master key secret is not valid");
            }

            var signedAtalaOperation = SignAtalaOperation(masterKeySecrets.Bytes, masterKeySecrets.KeyId, atalaOperation, _cryptoService);

            return new CreateSignedAtalaOperationForDeactivateDidResponse(signedAtalaOperation);
        }

        private static SignedAtalaOperation SignAtalaOperation(byte[] privateKey, string signedWith, AtalaOperation atalaOperation, ICryptoService cryptoService)
        {
            var signature = SignBytes(atalaOperation.ToByteString(), privateKey, cryptoService);
            return new SignedAtalaOperation()
            {
                SignedWith = signedWith,
                Signature = PrismEncoding.ByteArrayToByteString(signature),
                Operation = atalaOperation
            };
        }

        private static byte[] SignBytes(ByteString data, byte[] issuingPrivateKey, ICryptoService cryptoService)
        {
            return cryptoService.SignDataSecp256k1(PrismEncoding.ByteStringToByteArray(data), issuingPrivateKey);
        }
    }
}