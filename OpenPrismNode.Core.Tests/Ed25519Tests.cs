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
        byte[] signature = _cryptoService.SignDataEd25519(dataToSign, keyPair.PrivateKey.PrivateKey);

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

    [Fact]
    public void DeriveEd25519_SignAndVerify_ShouldSucceed_Simplified()
    {
        // Arrange
        // Use a fixed known mnemonic/seed if possible, otherwise generate one
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 0;
        PrismKeyUsage keyType = PrismKeyUsage.AuthenticationKey; // Index 3
        int keyIndex = 1; // Example key index
        string keyId = "auth-key-1";
        string curve = PrismParameters.Ed25519CurveName;

        byte[] dataToSign = Encoding.UTF8.GetBytes("Test Message should be longer bla bla"); // Simple, fixed message

        // Act
        // 1. Derive the Ed25519 key pair
        var keyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // --- Debugging Assertions ---
        keyPair.Should().NotBeNull();
        keyPair.PrivateKey.PrivateKey.Should().HaveCount(32, "Derived private key seed should be 32 bytes");
        keyPair.PublicKey.RawBytes.Should().NotBeNull().And.HaveCount(32, "Derived public key should be 32 bytes");

        // Explicitly check internal consistency again right here
        bool internalCheck = _cryptoService.CheckKeys(keyPair.PrivateKey.PrivateKey, keyPair.PublicKey.RawBytes!, curve);
        internalCheck.Should().BeTrue("Internal CheckKeys validation must pass before signing");
        // --- End Debugging Assertions ---

        // 2. Sign the data
        byte[] signature = null;
        Exception? signException = null;
        try
        {
            signature = _cryptoService.SignDataEd25519(dataToSign, keyPair.PrivateKey.PrivateKey);
        }
        catch (Exception ex)
        {
            signException = ex;
        }

        signException.Should().BeNull("Signing should not throw an exception");
        signature.Should().NotBeNullOrEmpty().And.HaveCount(64, "Ed25519 signature should be 64 bytes");

        // 3. Verify the signature
        bool isValid = false;
        Exception? verifyException = null;
        try
        {
            // Use the exact same byte arrays that were checked and used for signing
            isValid = _cryptoService.VerifyDataEd25519(dataToSign, signature, keyPair.PublicKey.RawBytes!);
        }
        catch (Exception ex)
        {
            verifyException = ex;
        }

        // Assert
        verifyException.Should().BeNull("Verification should not throw an exception");

        // --- THE FAILING ASSERTION ---
        isValid.Should().BeTrue("The signature should be valid for the given data and public key.");
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