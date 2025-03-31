// --- Tests/X25519Tests.cs ---
using Xunit;
using OpenPrismNode.Core.Services.Did;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Common;
using System.Linq;
using FluentAssertions; // Using FluentAssertions

namespace OpenPrismNode.Core.Tests;

using Models;

public class X25519Tests
{
    private readonly IKeyGenerationService _keyGenerationService;
    private readonly ICryptoService _cryptoService;

    public X25519Tests()
    {
        // Instantiate the concrete implementations for testing
        var secp256k1HdService = new HdKeyServiceNBitcoin();
        var ed25519HdService = new Slip0010DerivationServiceCardanoSharp();
        _cryptoService = new CryptoServiceBouncyCastle();

        _keyGenerationService = new KeyGenerationService(
            secp256k1HdService,
            ed25519HdService,
            _cryptoService);
    }

    [Fact]
    public void DeriveX25519_KeyAgreement_ShouldProduceSameSharedSecret()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 1; // Use a different DID index for variety
        PrismKeyUsage keyType = PrismKeyUsage.KeyAgreementKey; // Correct usage for X25519
        string curve = PrismParameters.X25519CurveName;

        // Alice's key
        int aliceKeyIndex = 10;
        string aliceKeyId = "key-agreement-alice";
        string aliceDerivationPath = $"m/{didIndex}'/{GetKeyTypeIndex(keyType)}'/{aliceKeyIndex}'"; // m/1'/2'/10'

        // Bob's key (different index on the same DID/seed)
        int bobKeyIndex = 11;
        string bobKeyId = "key-agreement-bob";
        string bobDerivationPath = $"m/{didIndex}'/{GetKeyTypeIndex(keyType)}'/{bobKeyIndex}'"; // m/1'/2'/11'

        // Act
        // 1. Derive Alice's X25519 key pair
        var aliceKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, aliceKeyIndex, aliceKeyId, curve);

        // 2. Derive Bob's X25519 key pair
        var bobKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, bobKeyIndex, bobKeyId, curve);

        // 3. Alice computes shared secret with Bob's public key
        byte[] aliceSharedSecret = _cryptoService.GenerateSharedSecretX25519(
            aliceKeyPair.PrivateKey.PrivateKey,
            bobKeyPair.PublicKey.RawBytes!); // Use Bob's public key

        // 4. Bob computes shared secret with Alice's public key
        byte[] bobSharedSecret = _cryptoService.GenerateSharedSecretX25519(
            bobKeyPair.PrivateKey.PrivateKey,      // Use Bob's private key
            aliceKeyPair.PublicKey.RawBytes!); // Use Alice's public key

        // Assert
        // Alice's Key
        aliceKeyPair.Should().NotBeNull();
        aliceKeyPair.PublicKey.Curve.Should().Be(PrismParameters.X25519CurveName);
        aliceKeyPair.PublicKey.KeyId.Should().Be(aliceKeyId);
        aliceKeyPair.KeyUsage.Should().Be(keyType);
        aliceKeyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        aliceKeyPair.PublicKey.RawBytes.Should().NotBeNull();
        aliceKeyPair.PublicKey.RawBytes.Should().HaveCount(32);

        // Bob's Key
        bobKeyPair.Should().NotBeNull();
        bobKeyPair.PublicKey.Curve.Should().Be(PrismParameters.X25519CurveName);
        bobKeyPair.PublicKey.KeyId.Should().Be(bobKeyId);
        bobKeyPair.KeyUsage.Should().Be(keyType);
        bobKeyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        bobKeyPair.PublicKey.RawBytes.Should().NotBeNull();
        bobKeyPair.PublicKey.RawBytes.Should().HaveCount(32);

        // Shared Secrets
        aliceSharedSecret.Should().NotBeNullOrEmpty();
        aliceSharedSecret.Should().HaveCount(32, "X25519 shared secret should be 32 bytes.");

        bobSharedSecret.Should().NotBeNullOrEmpty();
        bobSharedSecret.Should().HaveCount(32);

        // THE core assertion for key agreement:
        aliceSharedSecret.Should().Equal(bobSharedSecret,
            "Alice and Bob should derive the exact same shared secret.");
    }

    // Helper to get index consistent with KeyGenerationService
    private int GetKeyTypeIndex(PrismKeyUsage keyType) => keyType switch
    {
        PrismKeyUsage.MasterKey => 0,
        PrismKeyUsage.IssuingKey => 1,
        PrismKeyUsage.KeyAgreementKey => 2,
        PrismKeyUsage.AuthenticationKey => 3,
        PrismKeyUsage.CapabilityInvocationKey => 5,
        PrismKeyUsage.CapabilityDelegationKey => 6,
        _ => throw new NotSupportedException($"KeyUsage '{keyType}' has no defined index.")
    };
}