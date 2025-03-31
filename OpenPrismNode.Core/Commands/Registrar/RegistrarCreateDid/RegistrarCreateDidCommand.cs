namespace OpenPrismNode.Core.Commands.Registrar.RegistrarCreateDid
{
    using Common;
    using Crypto;
    using FluentResults;
    using Google.Protobuf;
    using Google.Protobuf.Collections;
    using MediatR;
    using Models;
    using Services.Did;

    /// <summary>
    /// MediatR command to handle the creation of a DID via the registrar.
    /// </summary>
    public class RegistrarCreateDidCommand : IRequestHandler<RegistrarCreateDidRequest, Result<RegistrarResponseDto>>
    {
        private IKeyGenerationService _keyGenerationService;
        private ISha256Service _sha256Service;
        private ICryptoService _cryptoService;

        /// <summary>
        /// Creates a new instance of the RegistrarCreateDidCommand.
        /// </summary>
        public RegistrarCreateDidCommand(
            IKeyGenerationService keyGenerationService,
            ISha256Service sha256Service,
            ICryptoService cryptoService)
        {
            _keyGenerationService = keyGenerationService;
            _sha256Service = sha256Service;
            _cryptoService = cryptoService;
        }

        public async Task<Result<RegistrarResponseDto>> Handle(RegistrarCreateDidRequest request, CancellationToken cancellationToken)
        {
            var template = new PrismDidTemplate();
            template.Mnemonic = _keyGenerationService.GenerateRandomMnemonic();
            (template.MasterKeyPair, template.SeedAsHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(template.Mnemonic);
            if (request.Secret?.VerificationMethod is null)
            {
                return Result.Fail("Verification method is required");
            }

            foreach (var verificationMethod in request.Secret.VerificationMethod)
            {
                if (string.IsNullOrEmpty(verificationMethod.Curve))
                {
                    return Result.Fail("Curve is required");
                }

                var keyId = verificationMethod.Id!;
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

                var key = _keyGenerationService.DeriveKeyFromSeed(template.SeedAsHex, 0, keyType, 0, keyId, verificationMethod.Curve);
                template.KeyPairs.Add(keyId, key);
            }

            RepeatedField<string> context = new RepeatedField<string>();
            if (request.DidDocument.Context is not null && request.DidDocument.Context.Any())
            {
                foreach (var contextString in request.DidDocument.Context)
                {
                    context.Add(contextString);
                }
            }
            else
            {
                context.Add(PrismParameters.JsonLdDefaultContext);
                context.Add(PrismParameters.JsonLdJsonWebKey2020);

                if (request.DidDocument.Service is not null && request.DidDocument.Service.Any(p => p.Type.Contains(PrismParameters.ServiceTypeLinkedDomains)))
                {
                    context.Add(PrismParameters.JsonLdDidCommMessaging);
                }

                if (request.DidDocument.Service is not null && request.DidDocument.Service.Any(p => p.Type.Contains(PrismParameters.ServiceTypeLinkedDomains)))
                {
                    context.Add(PrismParameters.JsonLdLinkedDomains);
                }
            }


            RepeatedField<PublicKey> publicKeys = new RepeatedField<PublicKey>();
            foreach (var keyPair in template.KeyPairs)
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

                publicKeys.Add(new PublicKey()
                {
                    Id = keyPair.Key,
                    Usage = keyUsage,
                    CompressedEcKeyData = CompressPublicKey(keyPair.Value.PublicKey.X, keyPair.Value.PublicKey.Y, keyPair.Value.PublicKey.Curve)
                });
            }

            RepeatedField<Service> services = new RepeatedField<Service>();
            if (request.DidDocument.Service is not null)
            {
                foreach (var service in request.DidDocument.Service)
                {
                    services.Add(new Service()
                    {
                        Id = service.Id,
                        Type = service.Type,
                        ServiceEndpoint = service.ServiceEndpoint,
                    });
                }
            }

            var atalaOperation =
                new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { context },
                            PublicKeys = { publicKeys },
                            Services = { services }
                        }
                    }
                };

            var encodedAtalaOperation = PrismEncoding.ByteStringToByteArray(atalaOperation.ToByteString());
            var hashedAtalaOperation = new Hash(_sha256Service).Of(encodedAtalaOperation);
            template.Identifier = "did:prism:" + PrismEncoding.ByteArrayToHex(hashedAtalaOperation.Value);

            var signedAtalaOperation = SignAtalaOperation(template.MasterKeyPair.PrivateKey.PrivateKey, template.MasterKeyPair.PublicKey.KeyId, atalaOperation, _cryptoService);
            return null;
        }

        private static CompressedECKeyData CompressPublicKey(byte[] x, byte[] y, string curve)
        {
            if (curve != "secp256k1")
            {
                throw new Exception("Only secp256k1 is supported");
            }

            byte[] newArray = new byte[x.Length + 1];
            x.CopyTo(newArray, 1);
            newArray[0] = (byte)(2 + (y[^1] & 1));
            var pk = new CompressedECKeyData()
            {
                Curve = "secp256k1",
                Data = PrismEncoding.ByteArrayToByteString(newArray),
            };
            return pk;
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