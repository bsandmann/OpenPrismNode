namespace OpenPrismNode.Core.Services.Did;

using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models; // Assuming PrismDidTemplate, Result etc. are here
using System;
using System.Collections.Generic;
using System.Text; // For Encoding

public sealed class KeyGenerationService : IKeyGenerationService // Assuming this interface exists
{
    private readonly IHdKeyService _secp256k1HdService; // NBitcoin implementation
    private readonly ISlip0010DerivationService _ed25519HdService; // CardanoSharp implementation
    private readonly ICryptoService _cryptoService; // BouncyCastle implementation

    public KeyGenerationService(
        IHdKeyService secp256k1HdService,
        ISlip0010DerivationService ed25519HdService,
        ICryptoService cryptoService)
    {
        _secp256k1HdService = secp256k1HdService ?? throw new ArgumentNullException(nameof(secp256k1HdService));
        _ed25519HdService = ed25519HdService ?? throw new ArgumentNullException(nameof(ed25519HdService));
        _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
    }

    public List<string> GenerateRandomMnemonic()
    {
        // Assuming NBitcoin service provides this
        return _secp256k1HdService.GenerateRandomMnemonic();
    }

    public (PrismKeyPair prismKeyPair, string seedHex) GenerateMasterKeyFromMnemonic(List<string> mnemonic)
    {
        // Consider making passphrase more secure or configurable
        var passphrase = "passphrase";
        // Use NBitcoin service to get the standard BIP39 seed
        var seedHex = _secp256k1HdService.DeriveSeed(mnemonic, passphrase);

        // Master key is typically secp256k1 in many DID methods, confirm for PRISM spec
        // Assuming master key is secp256k1 derived at index 0
        var masterKey = DeriveKeyFromSeed(seedHex, 0, PrismKeyUsage.MasterKey, 0, "master0", PrismParameters.Secp256k1CurveName);

        return (masterKey, seedHex);
    }

    // Main derivation logic
  public PrismKeyPair DeriveKeyFromSeed(string seedHex, int didIndex, PrismKeyUsage keyType, int keyIndex, string keyId, string curve)
    {
        var keyTypeIndex = GetKeyTypeIndex(keyType);
        var path = $"m/{didIndex}'/{keyTypeIndex}'/{keyIndex}'";

        byte[] privateKeyBytes;
        // byte[] publicKeyBytes; // We derive this using BouncyCastle now for Ed/X25519
        PrismPublicKey publicKey;

        switch (curve)
        {
            case PrismParameters.Secp256k1CurveName:
                (privateKeyBytes, byte[] secpPublicKeyBytes) = _secp256k1HdService.DeriveChildKeys(seedHex, path);
                if (privateKeyBytes.Length != 32 || secpPublicKeyBytes.Length != 65 || secpPublicKeyBytes[0] != 0x04)
                {
                    throw new InvalidOperationException($"Invalid key format returned from secp256k1 derivation for path {path}.");
                }
                var pkDeconstructed = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(secpPublicKeyBytes));
                 if (!pkDeconstructed.IsSuccess) throw new InvalidOperationException("Failed to deconstruct secp256k1 public key.");
                publicKey = new PrismPublicKey(keyType, keyId, PrismParameters.Secp256k1CurveName, pkDeconstructed.Value.x, pkDeconstructed.Value.y);
                break;

            case PrismParameters.Ed25519CurveName:
                // 1. Derive only the private key using CardanoSharp/SLIP-0010 service
                privateKeyBytes = _ed25519HdService.DerivePrivateKey(seedHex, path);
                if (privateKeyBytes.Length != 32)
                {
                     throw new InvalidOperationException($"Invalid private key length returned from Ed25519 derivation for path {path}: {privateKeyBytes.Length} bytes.");
                }
                // 2. Derive the corresponding public key using BouncyCastle/CryptoService
                byte[] edPublicKeyBytes = _cryptoService.GetEd25519PublicKeyFromPrivateKey(privateKeyBytes);
                 if (edPublicKeyBytes.Length != 32)
                 {
                     throw new InvalidOperationException($"Invalid public key length derived by CryptoService for Ed25519: {edPublicKeyBytes.Length} bytes.");
                 }
                publicKey = new PrismPublicKey(keyType, keyId, PrismParameters.Ed25519CurveName, edPublicKeyBytes);
                break;

            case PrismParameters.X25519CurveName:
                // 1. Derive the corresponding Ed25519 private key first
                byte[] ed25519PrivateKey = _ed25519HdService.DerivePrivateKey(seedHex, path);
                 if (ed25519PrivateKey.Length != 32) throw new InvalidOperationException($"Invalid Ed25519 private key format for path {path}.");

                // 2. Convert Ed25519 private key to X25519 private key (using BouncyCastle)
                privateKeyBytes = _cryptoService.ConvertEd25519PrivateKeyToX25519(ed25519PrivateKey);

                // 3. Derive X25519 public key from the X25519 private key (using BouncyCastle)
                byte[] xPublicKeyBytes = _cryptoService.GetX25519PublicKeyFromPrivateKey(privateKeyBytes);
                 if (xPublicKeyBytes.Length != 32) throw new InvalidOperationException($"Invalid X25519 public key format derived for path {path}.");

                publicKey = new PrismPublicKey(keyType, keyId, PrismParameters.X25519CurveName, xPublicKeyBytes);
                break;

            default:
                throw new NotSupportedException($"Curve '{curve}' is not supported for key derivation.");
        }

        // Perform key check - This should now pass consistently for Ed25519 as well
        // because the publicKey object was created using BouncyCastle's derivation,
        // which is the same method CheckKeys uses internally for comparison.
        var check = _cryptoService.CheckKeys(privateKeyBytes, publicKey.GetBytes(), curve);
        if (!check)
        {
            // This would indicate a deeper issue if it still fails now
            throw new Exception($"Key generation unsuccessful: Derived key pair failed validation for curve {curve}, path {path}, keyId {keyId}.");
        }

        return new PrismKeyPair(
            keyUsage: keyType,
            privateKey: new PrismPrivateKey(privateKeyBytes),
            publicKey: publicKey
        );
    }

    private int GetKeyTypeIndex(PrismKeyUsage keyType) => keyType switch
    {
        PrismKeyUsage.MasterKey => 0,
        PrismKeyUsage.IssuingKey => 1,
        PrismKeyUsage.KeyAgreementKey => 2,
        PrismKeyUsage.AuthenticationKey => 3,
        PrismKeyUsage.RevocationKey => 4, // Assuming index 4
        PrismKeyUsage.CapabilityInvocationKey => 5,
        PrismKeyUsage.CapabilityDelegationKey => 6,
        _ => throw new NotSupportedException($"KeyUsage '{keyType}' has no defined index.")
    };
}