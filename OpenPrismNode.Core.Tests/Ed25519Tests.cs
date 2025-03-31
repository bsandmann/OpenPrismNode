// --- Tests/Ed25519Tests.cs ---
using Xunit;
using OpenPrismNode.Core.Services.Did;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Common;
using System.Text;
using System.Linq;
using FluentAssertions; // Using FluentAssertions for expressive asserts

namespace OpenPrismNode.Core.Tests;

using Models;

public class Ed25519Tests
{
    private readonly IKeyGenerationService _keyGenerationService;
    private readonly ICryptoService _cryptoService;

    public Ed25519Tests()
    {
        // Instantiate the concrete implementations for testing
        // In a real app with DI, you might mock dependencies, but here we test the integration.
        var secp256k1HdService = new HdKeyServiceNBitcoin();
        var ed25519HdService = new Slip0010DerivationServiceCustom();
        _cryptoService = new CryptoServiceBouncyCastle(); // Keep instance for direct crypto ops

        _keyGenerationService = new KeyGenerationService(
            secp256k1HdService,
            ed25519HdService,
            _cryptoService);
    }

    [Fact]
    public void DeriveEd25519_SignAndVerify_ShouldSucceed()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic); // Need the seed

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.AuthenticationKey; // Example usage
        int keyIndex = 5; // Example key index
        string keyId = "auth-key-1";
        string curve = PrismParameters.Ed25519CurveName;
        string derivationPath = $"m/{didIndex}'/{GetKeyTypeIndex(keyType)}'/{keyIndex}'"; // m/0'/3'/5'

        byte[] dataToSign = Encoding.UTF8.GetBytes("This is the data to be signed with Ed25519.");

        // Act
        // 1. Derive the Ed25519 key pair
        var keyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // 2. Sign the data using the derived private key
        byte[] signature = _cryptoService.SignDataEd25519(keyPair.PrivateKey.PrivateKey, dataToSign);

        // 3. Verify the signature using the derived public key
        bool isValid = _cryptoService.VerifyDataEd25519(dataToSign, signature, keyPair.PublicKey.RawBytes!);

        // Assert
        keyPair.Should().NotBeNull();
        keyPair.PublicKey.Curve.Should().Be(PrismParameters.Ed25519CurveName);
        keyPair.PublicKey.KeyId.Should().Be(keyId);
        keyPair.KeyUsage.Should().Be(keyType);
        keyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        keyPair.PublicKey.RawBytes.Should().NotBeNull();
        keyPair.PublicKey.RawBytes.Should().HaveCount(32);
        keyPair.PublicKey.X.Should().BeNull(); // Ed25519 uses RawBytes
        keyPair.PublicKey.Y.Should().BeNull(); // Ed25519 uses RawBytes

        signature.Should().NotBeNullOrEmpty();
        // Ed25519 signatures are typically 64 bytes
        signature.Should().HaveCount(64);

        isValid.Should().BeTrue("The signature should be valid for the given data and public key.");

        // Optional: Verify with incorrect data fails
        byte[] incorrectData = Encoding.UTF8.GetBytes("This is NOT the data signed.");
        bool isInvalid = _cryptoService.VerifyDataEd25519(incorrectData, signature, keyPair.PublicKey.RawBytes!);
        isInvalid.Should().BeFalse("Verification should fail with incorrect data.");
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