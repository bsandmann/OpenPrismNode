using Xunit;
using OpenPrismNode.Core.Services.Did;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Common;
using System.Linq;
using FluentAssertions;

namespace OpenPrismNode.Core.Tests;

using System.Security.Cryptography;
using CardanoSharp.Wallet.Extensions.Models;
using Models;

/// <summary>
/// Tests focusing on the key derivation process and basic properties,
/// similar to the old tests, using the new service structure.
/// Note: DeriveKeyFromSeed internally calls CheckKeys, so these might still
/// fail occasionally if an invalid scalar is derived for secp256k1.
/// </summary>
public class BasicDerivationTests
{
    private readonly IKeyGenerationService _keyGenerationService;
    // We don't necessarily need ICryptoService directly in these specific tests

    public BasicDerivationTests()
    {
        // Instantiate the concrete implementations
        var secp256k1HdService = new HdKeyServiceNBitcoin();
        var ed25519HdService = new Slip0010DerivationServiceCardanoSharp();
        var cryptoService = new CryptoServiceBouncyCastle(); // Still needed by KeyGenerationService

        _keyGenerationService = new KeyGenerationService(
            secp256k1HdService,
            ed25519HdService,
            cryptoService);
    }

    // Test similar to old 'MasterKeyGenerationWorksAsExpected'
    [Fact]
    public void GenerateMasterKey_Secp256k1_ShouldProduceValidStructure()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();

        // Act
        // GenerateMasterKeyFromMnemonic implicitly derives the secp256k1 master key
        (PrismKeyPair keyPair, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        // Assert
        mnemonic.Should().HaveCount(24);
        seedHex.Should().NotBeNullOrEmpty();
        seedHex.Length.Should().Be(128); // 64 bytes * 2 hex chars

        keyPair.Should().NotBeNull();
        keyPair.KeyUsage.Should().Be(PrismKeyUsage.MasterKey);

        // Private Key Checks
        keyPair.PrivateKey.Should().NotBeNull();
        keyPair.PrivateKey.PrivateKey.Should().HaveCount(32);

        // Public Key Checks (Secp256k1 specific)
        keyPair.PublicKey.Should().NotBeNull();
        keyPair.PublicKey.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        keyPair.PublicKey.KeyId.Should().Be("master0"); // As defined in GenerateMasterKeyFromMnemonic
        keyPair.PublicKey.X.Should().NotBeNull().And.HaveCount(32);
        keyPair.PublicKey.Y.Should().NotBeNull().And.HaveCount(32);
        keyPair.PublicKey.RawBytes.Should().BeNull();

        // Check combined public key format
        byte[] combinedPublicKey = keyPair.PublicKey.GetBytes();
        combinedPublicKey.Should().HaveCount(65);
        combinedPublicKey[0].Should().Be(0x04); // Uncompressed prefix

        // Optional: Deconstruct from hex (similar to old test)
        var hex = PrismEncoding.ByteArrayToHex(combinedPublicKey);
        var deconstructedResult = PrismEncoding.HexToPublicKeyPairByteArrays(hex);
        deconstructedResult.IsSuccess.Should().BeTrue();
        deconstructedResult.Value.x.Should().Equal(keyPair.PublicKey.X);
        deconstructedResult.Value.y.Should().Equal(keyPair.PublicKey.Y);
    }

    // Test similar to old 'SecondaryKeyDerivation'
    [Fact]
    public void DeriveSecondaryKey_Secp256k1_ShouldProduceValidStructure()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.IssuingKey; // assertionMethod
        int keyIndex = 0;
        string keyId = "issuing0";
        string curve = PrismParameters.Secp256k1CurveName;

        // Act
        var secondaryKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // Assert
        secondaryKeyPair.Should().NotBeNull();
        secondaryKeyPair.KeyUsage.Should().Be(keyType);

        // Private Key Checks
        secondaryKeyPair.PrivateKey.Should().NotBeNull();
        secondaryKeyPair.PrivateKey.PrivateKey.Should().HaveCount(32);

        // Public Key Checks (Secp256k1 specific)
        secondaryKeyPair.PublicKey.Should().NotBeNull();
        secondaryKeyPair.PublicKey.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        secondaryKeyPair.PublicKey.KeyId.Should().Be(keyId);
        secondaryKeyPair.PublicKey.X.Should().NotBeNull().And.HaveCount(32);
        secondaryKeyPair.PublicKey.Y.Should().NotBeNull().And.HaveCount(32);
        secondaryKeyPair.PublicKey.RawBytes.Should().BeNull();

        // Check combined public key format
        byte[] combinedPublicKey = secondaryKeyPair.PublicKey.GetBytes();
        combinedPublicKey.Should().HaveCount(65);
        combinedPublicKey[0].Should().Be(0x04); // Uncompressed prefix
    }

    // Test similar to old 'MasterKeyGeneration_is_deterministic'
    [Fact]
    public void DeriveKey_Secp256k1_ShouldBeDeterministic()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        // Derive seed once
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 1;
        PrismKeyUsage keyType = PrismKeyUsage.AuthenticationKey;
        int keyIndex = 2;
        string keyId = "auth-deterministic";
        string curve = PrismParameters.Secp256k1CurveName;

        // Act
        // Derive the same key twice using the same parameters
        var keyPair1 = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);
        var keyPair2 = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // Assert
        keyPair1.Should().NotBeNull();
        keyPair2.Should().NotBeNull();

        // Check Private Keys are identical
        keyPair1.PrivateKey.PrivateKey.Should().Equal(keyPair2.PrivateKey.PrivateKey);

        // Check Public Keys are identical (byte level)
        keyPair1.PublicKey.GetBytes().Should().Equal(keyPair2.PublicKey.GetBytes());

        // Check individual components for completeness
        keyPair1.PublicKey.X.Should().Equal(keyPair2.PublicKey.X);
        keyPair1.PublicKey.Y.Should().Equal(keyPair2.PublicKey.Y);
        keyPair1.PublicKey.Curve.Should().Be(keyPair2.PublicKey.Curve);
        keyPair1.PublicKey.KeyId.Should().Be(keyPair2.PublicKey.KeyId);
        keyPair1.KeyUsage.Should().Be(keyPair2.KeyUsage);
    }

    // Add similar tests for Ed25519 and X25519 derivation structure/determinism if desired

    [Fact]
    public void DeriveSecondaryKey_Ed25519_ShouldProduceValidStructure()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.AuthenticationKey;
        int keyIndex = 1;
        string keyId = "auth-ed25519-1";
        string curve = PrismParameters.Ed25519CurveName;

        // Act
        var secondaryKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // Assert
        secondaryKeyPair.Should().NotBeNull();
        secondaryKeyPair.KeyUsage.Should().Be(keyType);
        secondaryKeyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        secondaryKeyPair.PublicKey.Curve.Should().Be(PrismParameters.Ed25519CurveName);
        secondaryKeyPair.PublicKey.KeyId.Should().Be(keyId);
        secondaryKeyPair.PublicKey.RawBytes.Should().NotBeNull().And.HaveCount(32);
        secondaryKeyPair.PublicKey.X.Should().BeNull();
        secondaryKeyPair.PublicKey.Y.Should().BeNull();
        secondaryKeyPair.PublicKey.GetBytes().Should().HaveCount(32); // Raw format
    }

    [Fact]
    public void DeriveSecondaryKey_X25519_ShouldProduceValidStructure()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.KeyAgreementKey;
        int keyIndex = 2;
        string keyId = "keyagree-x25519-1";
        string curve = PrismParameters.X25519CurveName;

        // Act
        var secondaryKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // Assert
        secondaryKeyPair.Should().NotBeNull();
        secondaryKeyPair.KeyUsage.Should().Be(keyType);
        secondaryKeyPair.PrivateKey.PrivateKey.Should().HaveCount(32); // Clamped X25519 key
        secondaryKeyPair.PublicKey.Curve.Should().Be(PrismParameters.X25519CurveName);
        secondaryKeyPair.PublicKey.KeyId.Should().Be(keyId);
        secondaryKeyPair.PublicKey.RawBytes.Should().NotBeNull().And.HaveCount(32);
        secondaryKeyPair.PublicKey.X.Should().BeNull();
        secondaryKeyPair.PublicKey.Y.Should().BeNull();
        secondaryKeyPair.PublicKey.GetBytes().Should().HaveCount(32); // Raw format
    }

    [Fact]
    public void DeriveSecondaryKey_Ed25519_StepByStep_ShouldProduceValidStructure()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);
        byte[] seedBytes = Convert.FromHexString(seedHex);

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.AuthenticationKey; // Index 3
        int keyIndex = 1;
        string keyId = "auth-ed25519-1";
        string curve = PrismParameters.Ed25519CurveName;

        uint hardenedDidIndex = (uint)didIndex | 0x80000000;
        uint hardenedKeyTypeIndex = (uint)GetKeyTypeIndex(keyType) | 0x80000000;
        uint hardenedKeyIndex = (uint)keyIndex | 0x80000000;

        // --- Corrected SLIP-0010 Master Node Setup ---
        byte[] masterScalarBytes = seedBytes[..32];       // Left 32 bytes of seed = k_m
        byte[] masterChainCodeBytes = seedBytes[32..];    // Right 32 bytes of seed = c_m

        // Calculate the "right half" (nonce) for the extended master key
        // Typically done by hashing the master scalar (k_m) with SHA512
        byte[] scalarHash;
        using (var sha512 = SHA512.Create())
        {
            scalarHash = sha512.ComputeHash(masterScalarBytes); // Hash the 32-byte scalar
        }
        // The right half is the last 32 bytes of the 64-byte SHA512 hash
        byte[] masterNonceBytes = scalarHash[32..];

        // Combine scalar and nonce to form the 64-byte extended master key
        byte[] extendedMasterKeyBytes = masterScalarBytes.Concat(masterNonceBytes).ToArray();
        extendedMasterKeyBytes.Length.Should().Be(64, "Extended master key must be 64 bytes");

        // Create the PrivateKey object using the 64-byte extended key and 32-byte chain code
        var masterNodePrivateKey = new CardanoSharp.Wallet.Models.Keys.PrivateKey(extendedMasterKeyBytes, masterChainCodeBytes);
        // --- End Corrected Setup ---

        // Act & Assert Step-by-Step
        CardanoSharp.Wallet.Models.Keys.PrivateKey derivedNode1 = null!;
        CardanoSharp.Wallet.Models.Keys.PrivateKey derivedNode2 = null!;
        CardanoSharp.Wallet.Models.Keys.PrivateKey finalDerivedNode = null!;
        Exception? derivationException = null;

        try
        {
            // Step 1: Derive m/0'
            Console.WriteLine("Attempting to derive m/0'...");
            derivedNode1 = CardanoSharp.Wallet.Utilities.Bip32Utility.GetChildKeyDerivation(masterNodePrivateKey, hardenedDidIndex);
            derivedNode1.Should().NotBeNull("Derivation m/0' should succeed");
            derivedNode1.Key.Should().HaveCount(64, "Intermediate key m/0' should be 64 bytes");
            derivedNode1.Chaincode.Should().HaveCount(32, "Intermediate chain code m/0' should be 32 bytes");
            Console.WriteLine($"Derived m/0' successfully. Key starts with: {Convert.ToHexString(derivedNode1.Key.Take(4).ToArray())}...");

            // Step 2: Derive m/0'/3'
            Console.WriteLine("Attempting to derive m/0'/3'...");
            derivedNode2 = CardanoSharp.Wallet.Utilities.Bip32Utility.GetChildKeyDerivation(derivedNode1, hardenedKeyTypeIndex);
            derivedNode2.Should().NotBeNull("Derivation m/0'/3' should succeed");
            derivedNode2.Key.Should().HaveCount(64, "Intermediate key m/0'/3' should be 64 bytes");
            derivedNode2.Chaincode.Should().HaveCount(32, "Intermediate chain code m/0'/3' should be 32 bytes");
            Console.WriteLine($"Derived m/0'/3' successfully. Key starts with: {Convert.ToHexString(derivedNode2.Key.Take(4).ToArray())}...");


            // Step 3: Derive m/0'/3'/1'
            Console.WriteLine("Attempting to derive m/0'/3'/1'...");
            finalDerivedNode = CardanoSharp.Wallet.Utilities.Bip32Utility.GetChildKeyDerivation(derivedNode2, hardenedKeyIndex);
            finalDerivedNode.Should().NotBeNull("Derivation m/0'/3'/1' should succeed");
            finalDerivedNode.Key.Should().HaveCount(64, "Final derived key should be 64 bytes");
            finalDerivedNode.Chaincode.Should().HaveCount(32, "Final chain code should be 32 bytes");
            Console.WriteLine($"Derived m/0'/3'/1' successfully. Key starts with: {Convert.ToHexString(finalDerivedNode.Key.Take(4).ToArray())}...");

        }
        catch (Exception ex)
        {
            derivationException = ex;
            // Use a more detailed output, including the stack trace
            Console.WriteLine($"Derivation failed: {ex.GetType().Name} - {ex.Message}\nStackTrace:\n{ex.StackTrace}");
        }

        // Final Assertions
        derivationException.Should().BeNull($"Derivation should succeed step-by-step. Failed with: {derivationException?.Message}");

        if (finalDerivedNode != null)
        {
            byte[] finalPrivateKeyBytes = finalDerivedNode.Key[..32]; // Extract final 32-byte scalar
            var finalPublicKey = finalDerivedNode.GetPublicKey(withZeroByte: false);
            byte[] finalPublicKeyBytes = finalPublicKey.Key;

            finalPrivateKeyBytes.Should().HaveCount(32);
            finalPublicKeyBytes.Should().HaveCount(32);

            var finalKeyPair = new PrismKeyPair(
                 keyType,
                 new PrismPrivateKey(finalPrivateKeyBytes),
                 new PrismPublicKey(keyType, keyId, curve, finalPublicKeyBytes)
             );

            finalKeyPair.KeyUsage.Should().Be(keyType);
            finalKeyPair.PublicKey.Curve.Should().Be(PrismParameters.Ed25519CurveName);
            finalKeyPair.PublicKey.KeyId.Should().Be(keyId);
            finalKeyPair.PublicKey.RawBytes.Should().NotBeNull().And.HaveCount(32);
            finalKeyPair.PublicKey.GetBytes().Should().HaveCount(32);
        }
        else
        {
            // Force test failure if derivation failed and exception wasn't null
             true.Should().BeFalse("Final derived node was null, indicating derivation failure.");
        }
    }

// Helper to get index consistent with KeyGenerationService
    private int GetKeyTypeIndex(PrismKeyUsage keyType) => keyType switch
    {
        PrismKeyUsage.MasterKey => 0,
        PrismKeyUsage.IssuingKey => 1,
        PrismKeyUsage.KeyAgreementKey => 2,
        PrismKeyUsage.AuthenticationKey => 3,
        PrismKeyUsage.RevocationKey => 4,
        PrismKeyUsage.CapabilityInvocationKey => 5,
        PrismKeyUsage.CapabilityDelegationKey => 6,
        _ => throw new NotSupportedException($"KeyUsage '{keyType}' has no defined index.")
    };
}