namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForUpdateDid
{
    using System.Text.Json;
    using CardanoSharp.Wallet.Models.Keys;
    using Common;
    using CreateSignedAtalaOperationForCreateDid;
    using FluentResults;
    using GetOperationStatusByOperationHash;
    using GetVerificationMethodSecrets;
    using Google.Protobuf;
    using Google.Protobuf.Collections;
    using MediatR;
    using Models;
    using OpenPrismNode.Core.Crypto;
    using OpenPrismNode.Core.Services.Did;
    using ResolveDid;
    using PublicKey = OpenPrismNode.PublicKey;

    /// <summary>
    /// MediatR command to handle the creation of a DID via the registrar.
    /// </summary>
    public class CreateSignedAtalaOperationForUpdateDidHandler : IRequestHandler<CreateSignedAtalaOperationForUpdateDidRequest, Result<CreateSignedAtalaOperationForUpdateDidResponse>>
    {
        private IKeyGenerationService _keyGenerationService;
        private ISha256Service _sha256Service;
        private ICryptoService _cryptoService;
        private IMediator _mediator;

        /// <summary>
        /// Creates a new instance of the RegistrarCreateDidCommand.
        /// </summary>
        public CreateSignedAtalaOperationForUpdateDidHandler(
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

        public async Task<Result<CreateSignedAtalaOperationForUpdateDidResponse>> Handle(CreateSignedAtalaOperationForUpdateDidRequest request, CancellationToken cancellationToken)
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

            var lastOperationHash = resolveDidResult.Value.LastOperationHash;
            var masterKeyPair = resolveDidResult.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyUsage == PrismKeyUsage.MasterKey);
            if (masterKeyPair is null)
            {
                return Result.Fail("Public Master key pair not found");
            }

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

            var template = new PrismDidTemplate();
            var updateDidActions = new RepeatedField<UpdateDIDAction>();
            for (var index = 0; index < request.Operations.Count; index++)
            {
                RepeatedField<PublicKey> publicKeys = new RepeatedField<PublicKey>();
                List<string> keyIdsToRemvoe = new List<string>();
                List<Service> servicesToAdd = new List<Service>();
                List<string> servicesToRemove = new List<string>();
                List<(string, string, string)> servicesToUpdate = new List<(string, string, string)>();
                List<string> contextsToPatch = new List<string>();
                var opsKey = new Dictionary<string, PrismKeyPair>();
                var operationType = request.Operations[index];
                if (operationType.Equals(PrismParameters.AddToDidDocument))
                {
                    var document = request.DidDocuments[index];
                    var verificationMethodIds = new List<string>();
                    document.TryGetValue("verificationMethod", out var verificationMethodJson);
                    if (verificationMethodJson is null)
                    {
                        return Result.Fail("Verification method is required");
                    }

                    if (verificationMethodJson is JsonElement vmElement && vmElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in vmElement.EnumerateArray())
                        {
                            // Each 'item' is a JsonElement representing { "id": "did:..." }
                            if (item.TryGetProperty("id", out var idProperty))
                            {
                                var keyId = idProperty.GetString()!.Split('#')[idProperty.GetString()!.Split('#').Length - 1];
                                verificationMethodIds.Add(keyId);
                            }
                        }

                        if (verificationMethodIds.Count == 0)
                        {
                            return Result.Fail("No 'id' fields found in verificationMethod array.");
                        }
                    }
                    else
                    {
                        return Result.Fail("Verification method must be an array of objects.");
                    }

                    var verificationMethodPrivateDatas = new List<RegistrarVerificationMethodPrivateData>();
                    foreach (var verificationMethodPrivateData in request.Secret!.VerificationMethod)
                    {
                        if (verificationMethodIds.Contains(verificationMethodPrivateData.Id))
                        {
                            verificationMethodPrivateDatas.Add(verificationMethodPrivateData);
                        }
                    }

                    foreach (var verificationMethod in verificationMethodPrivateDatas)
                    {
                        if (string.IsNullOrEmpty(verificationMethod.Curve))
                        {
                            return Result.Fail("Curve is required");
                        }

                        var keyId = verificationMethod.Id!;
                        var keyIndex = 0;
                        PrismKeyUsage keyType = PrismKeyUsage.UnknownKey;
                        // This is a simplification, but it is unclear how this should work with the current implemenation of the PRISM method otherwise.
                        switch (verificationMethod.Purpose?.Single())
                        {
                            case "authentication":
                                keyType = PrismKeyUsage.AuthenticationKey;
                                break;
                            case "assertionMethod":
                                keyType = PrismKeyUsage.IssuingKey;
                                break;
                            case "keyAgreement":
                                keyType = PrismKeyUsage.KeyAgreementKey;
                                break;
                            case "capabilityInvocation":
                                keyType = PrismKeyUsage.CapabilityInvocationKey;
                                break;
                            case "capabilityDelegation":
                                keyType = PrismKeyUsage.CapabilityDelegationKey;
                                break;
                            default:
                                break;
                        }

                        var masterKeySeed = _keyGenerationService.GenerateMasterKeyFromMnemonic(masterKeySecrets.Mnemonic.Split(" ").ToList());
                        if (masterKeySeed.prismKeyPair.PrivateKey.PrivateKey.Equals(masterKeySecrets.Bytes))
                        {
                            // Sanity check
                            return Result.Fail("The master key seed is not valid");
                        }

                        var key = _keyGenerationService.DeriveKeyFromSeed(masterKeySeed.seedHex, 0, keyType, resolveDidResult.Value.InternalDidDocument.PublicKeys.Count + keyIndex, keyId, verificationMethod.Curve);
                        template.KeyPairs.Add(keyId, key);
                        opsKey.Add(keyId, key);
                        keyIndex++;
                    }


                    foreach (var keyPair in opsKey)
                    {
                        var keyUsage = keyPair.Value.KeyUsage switch
                        {
                            PrismKeyUsage.AuthenticationKey => KeyUsage.AuthenticationKey,
                            PrismKeyUsage.IssuingKey => KeyUsage.IssuingKey,
                            PrismKeyUsage.RevocationKey => KeyUsage.RevocationKey,
                            PrismKeyUsage.KeyAgreementKey => KeyUsage.KeyAgreementKey,
                            PrismKeyUsage.CapabilityInvocationKey => KeyUsage.CapabilityInvocationKey,
                            PrismKeyUsage.CapabilityDelegationKey => KeyUsage.CapabilityDelegationKey,
                            _ => throw new Exception("Unknown key usage")
                        };

                        if (keyPair.Value.PublicKey.Curve.Equals(PrismParameters.Secp256k1CurveName))
                        {
                            if (keyPair.Value.PublicKey.X is null || keyPair.Value.PublicKey.Y is null)
                            {
                                return Result.Fail("X and Y coordinates are required for secp256k1 keys");
                            }

                            publicKeys.Add(new PublicKey()
                            {
                                Id = keyPair.Key,
                                Usage = keyUsage,
                                CompressedEcKeyData = PrismPublicKey.CompressPublicKey(keyPair.Value.PublicKey.X, keyPair.Value.PublicKey.Y, keyPair.Value.PublicKey.Curve),
                            });
                        }
                        else if (keyPair.Value.PublicKey.Curve.Equals(PrismParameters.Ed25519CurveName) ||
                                 keyPair.Value.PublicKey.Curve.Equals(PrismParameters.X25519CurveName))
                        {
                            if (keyPair.Value.PublicKey.RawBytes is null)
                            {
                                return Result.Fail("Raw bytes are required for Ed25519 and X25519 keys");
                            }

                            publicKeys.Add(new PublicKey()
                            {
                                Id = keyPair.Key,
                                Usage = keyUsage,
                                CompressedEcKeyData = new CompressedECKeyData() { Curve = keyPair.Value.PublicKey.Curve, Data = PrismEncoding.ByteArrayToByteString(keyPair.Value.PublicKey.RawBytes) },
                            });
                        }
                    }
                }
                else if (operationType.Equals(PrismParameters.RemoveFromDidDocument))
                {
                    var document = request.DidDocuments[index];
                    var verificationMethodIds = new List<string>();
                    document.TryGetValue("verificationMethod", out var verificationMethodJson);
                    if (verificationMethodJson is null)
                    {
                        return Result.Fail("Verification method is required");
                    }

                    if (verificationMethodJson is JsonElement vmElement && vmElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in vmElement.EnumerateArray())
                        {
                            // Each 'item' is a JsonElement representing { "id": "did:..." }
                            if (item.TryGetProperty("id", out var idProperty))
                            {
                                var keyId = idProperty.GetString()!.Split('#')[idProperty.GetString()!.Split('#').Length - 1];
                                verificationMethodIds.Add(keyId);
                            }
                        }

                        if (verificationMethodIds.Count == 0)
                        {
                            return Result.Fail("No 'id' fields found in verificationMethod array.");
                        }
                    }
                    else
                    {
                        return Result.Fail("Verification method must be an array of objects.");
                    }

                    foreach (var verificationMethodId in verificationMethodIds)
                    {
                        var referencedInSecret = request.Secret?.VerificationMethod?.FirstOrDefault(p => p.Id == verificationMethodId);
                        if (referencedInSecret is not null)
                        {
                            var addedInPublicKeys = template.KeyPairs.Keys.FirstOrDefault(p => p.Equals(verificationMethodId));
                            if (addedInPublicKeys is not null)
                            {
                                // It was first added to be added and should now be removed. As this was defined as a valid operation we leave it at that and remove it
                                keyIdsToRemvoe.Add(verificationMethodId);
                            }
                            else
                            {
                                // It was referenced, but not added. So the remove operation either comes before adding or there is some other misalignment.
                                return Result.Fail("The verification method is referenced to be removed, but not added already.. This is not allowed.");
                            }
                        }
                        else
                        {
                            var keyInDid = resolveDidResult.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyId == verificationMethodId);
                            if (keyInDid is not null)
                            {
                                if (keyInDid.KeyUsage == PrismKeyUsage.MasterKey)
                                {
                                    return Result.Fail("A masterkey cannot be removed.");
                                }
                                else if (keyInDid.KeyUsage == PrismKeyUsage.UnknownKey || keyInDid.KeyUsage == PrismKeyUsage.RevocationKey)
                                {
                                    return Result.Fail("Unsupported key usage.");
                                }
                                else if (resolveDidResult.Value.InternalDidDocument.PublicKeys.Where(p => p.KeyUsage != PrismKeyUsage.MasterKey && p.KeyUsage != PrismKeyUsage.RevocationKey && p.KeyUsage != PrismKeyUsage.UnknownKey).ToList().Count == 1)
                                {
                                    // The last key is removed. This is not allowed.
                                    return Result.Fail("The last key cannot be removed. To deacticate a DID, use the DeactivateDid operation.");
                                }
                                else
                                {
                                    keyIdsToRemvoe.Add(verificationMethodId);
                                }
                            }
                            else
                            {
                                return Result.Fail($"The key '{verificationMethodId}' cannot be found for removal.");
                            }
                        }
                    }
                }
                else if (operationType.Equals(PrismParameters.SetDidDocument))
                {
                    var document = request.DidDocuments[index];
                    if (document.Services is not null)
                    {
                        var existingServices = resolveDidResult.Value.InternalDidDocument.PrismServices;
                        foreach (var registrarService in document.Services)
                        {
                            if (existingServices.Select(p => p.ServiceId).Contains(registrarService.Id))
                            {
                                // The service already exists, maybe we need to update it;
                                var existingService = existingServices.First(p => p.ServiceId == registrarService.Id);
                                if (!existingService.Type.Equals(registrarService.Type) || (existingService.ServiceEndpoints.Uri is not null && !existingService.ServiceEndpoints.Uri.AbsoluteUri.Equals(registrarService.ServiceEndpoint)))
                                {
                                    servicesToUpdate.Add((
                                        registrarService.Id, registrarService.Type, registrarService.ServiceEndpoint
                                    ));
                                }
                            }
                            else
                            {
                                // The service does not exist, so we need to add it;
                                servicesToAdd.Add(new Service()
                                {
                                    Id = registrarService.Id,
                                    Type = registrarService.Type,
                                    ServiceEndpoint = registrarService.ServiceEndpoint
                                });
                            }
                        }

                        foreach (var existingService in existingServices)
                        {
                            if (document.Services.Select(p => p.Id).Contains(existingService.ServiceId))
                            {
                                // The service already exists, we should have updated it. See above.
                            }
                            else
                            {
                                // the service is not in the list of the current services, so we need to remove it.
                                servicesToRemove.Add(existingService.ServiceId);
                            }
                        }
                    }

                    if (document.Context is not null)
                    {
                        contextsToPatch = document.Context;
                    }
                }

                foreach (var publicKey in publicKeys)
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        AddKey = new AddKeyAction()
                        {
                            Key = publicKey
                        }
                    });
                }

                foreach (var keyId in keyIdsToRemvoe)
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        RemoveKey = new RemoveKeyAction()
                        {
                            KeyId = keyId
                        }
                    });
                }

                foreach (var service in servicesToAdd)
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        AddService = new AddServiceAction()
                        {
                            Service = service
                        }
                    });
                }

                foreach (var serviceId in servicesToRemove)
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        RemoveService = new RemoveServiceAction()
                        {
                            ServiceId = serviceId
                        }
                    });
                }

                foreach (var (serviceId, type, endpoints) in servicesToUpdate)
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        UpdateService = new UpdateServiceAction()
                        {
                            ServiceId = serviceId,
                            Type = type,
                            ServiceEndpoints = endpoints
                        }
                    });
                }

                if (contextsToPatch.Any())
                {
                    updateDidActions.Add(new UpdateDIDAction()
                    {
                        PatchContext = new PatchContextAction()
                        {
                            Context = { contextsToPatch }
                        }
                    });
                }
            }

            var atalaOperation =
                new AtalaOperation
                {
                    UpdateDid = new UpdateDIDOperation()
                    {
                        Id = didIdentifier,
                        Actions = { updateDidActions },
                        PreviousOperationHash = PrismEncoding.ByteArrayToByteString(lastOperationHash.Value),
                    }
                };

            var signedAtalaOperation = SignAtalaOperation(masterKeySecrets.Bytes, masterKeySecrets.KeyId, atalaOperation, _cryptoService);

            return new CreateSignedAtalaOperationForUpdateDidResponse(signedAtalaOperation, template);
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