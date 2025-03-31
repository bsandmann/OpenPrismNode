// --- Tests/Secp256k1Tests.cs ---
using Xunit;
using OpenPrismNode.Core.Services.Did;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Common;
using System.Text;
using System.Linq;
using FluentAssertions; // Using FluentAssertions

namespace OpenPrismNode.Core.Tests;

using Models;

public class Secp256k1Tests
{
    private readonly IKeyGenerationService _keyGenerationService;
    private readonly ICryptoService _cryptoService;

    public Secp256k1Tests()
    {
        // Instantiate the concrete implementations for testing
        var secp256k1HdService = new HdKeyServiceNBitcoin();
        var ed25519HdService = new Slip0010DerivationServiceCardanoSharp(); // Needed by KeyGenerationService constructor
        _cryptoService = new CryptoServiceBouncyCastle(); // Keep instance for direct crypto ops

        _keyGenerationService = new KeyGenerationService(
            secp256k1HdService,
            ed25519HdService,
            _cryptoService);
    }

    [Fact]
    public void DeriveSecp256k1_SignAndVerify_DerSignature_ShouldSucceed()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic); // Need the seed

        int didIndex = 2; // Yet another index
        PrismKeyUsage keyType = PrismKeyUsage.CapabilityInvocationKey; // Example usage
        int keyIndex = 1; // Example key index
        string keyId = "cap-invoke-key-1";
        string curve = PrismParameters.Secp256k1CurveName;
        string derivationPath = $"m/{didIndex}'/{GetKeyTypeIndex(keyType)}'/{keyIndex}'"; // m/2'/5'/1'

        byte[] dataToSign = Encoding.UTF8.GetBytes("Data to be signed with secp256k1 (DER).");

        // Act
        // 1. Derive the secp256k1 key pair
        var keyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // 2. Sign the data using the derived private key (DER format)
        byte[] derSignature = _cryptoService.SignDataSecp256k1(keyPair.PrivateKey.PrivateKey, dataToSign);

        // 3. Verify the signature using the derived public key (DER format)
        // GetBytes() should return the 65-byte uncompressed key
        bool isValid = _cryptoService.VerifyDataSecp256k1(dataToSign, derSignature, keyPair.PublicKey.GetBytes());

        // Assert
        keyPair.Should().NotBeNull();
        keyPair.PublicKey.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        keyPair.PublicKey.KeyId.Should().Be(keyId);
        keyPair.KeyUsage.Should().Be(keyType);
        keyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        keyPair.PublicKey.X.Should().NotBeNull().And.HaveCount(32);
        keyPair.PublicKey.Y.Should().NotBeNull().And.HaveCount(32);
        keyPair.PublicKey.RawBytes.Should().BeNull(); // Secp256k1 uses X, Y
        keyPair.PublicKey.GetBytes().Should().HaveCount(65).And.StartWith(new byte[] { 0x04 }); // Uncompressed format

        derSignature.Should().NotBeNullOrEmpty();
        // DER signatures vary slightly in length (usually 70-72 bytes)
        derSignature.Length.Should().BeInRange(70, 72);
        _cryptoService.IsValidDerSignature(derSignature).Should().BeTrue();

        isValid.Should().BeTrue("The DER signature should be valid for the given data and public key.");

        // Optional: Verify with incorrect data fails
        byte[] incorrectData = Encoding.UTF8.GetBytes("This is NOT the data signed (DER).");
        bool isInvalid = _cryptoService.VerifyDataSecp256k1(incorrectData, derSignature, keyPair.PublicKey.GetBytes());
        isInvalid.Should().BeFalse("DER verification should fail with incorrect data.");
    }

    [Fact]
    public void DeriveSecp256k1_SignAndVerify_PlainSignature_ShouldSucceed()
    {
        // Arrange
        var mnemonic = _keyGenerationService.GenerateRandomMnemonic();
        (_, string seedHex) = _keyGenerationService.GenerateMasterKeyFromMnemonic(mnemonic);

        int didIndex = 1;
        PrismKeyUsage keyType = PrismKeyUsage.CapabilityDelegationKey;
        int keyIndex = 2;
        string keyId = "key-1";
        string curve = PrismParameters.Secp256k1CurveName;
        string derivationPath = $"m/{didIndex}'/{GetKeyTypeIndex(keyType)}'/{keyIndex}'"; // m/3'/6'/0'

        byte[] dataToSign = Encoding.UTF8.GetBytes("Data to be signed with secp256k1 (Plain R||S).");

        // Act
        // 1. Derive the secp256k1 key pair
        var keyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex, keyId, curve);

        // 2. Sign the data using the derived private key (Plain R||S format)
        byte[] plainSignature = _cryptoService.SignDataSecp256k1WithoutDER(keyPair.PrivateKey.PrivateKey, dataToSign);

        // 3. Verify the signature using the derived public key (Plain R||S format)
        bool isValid = _cryptoService.VerifyDataSecp256k1WithoutDER(dataToSign, plainSignature, keyPair.PublicKey.GetBytes());

        // Assert
        keyPair.Should().NotBeNull();
        keyPair.PublicKey.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        keyPair.PrivateKey.PrivateKey.Should().HaveCount(32);
        keyPair.PublicKey.GetBytes().Should().HaveCount(65);

        plainSignature.Should().NotBeNullOrEmpty();
        // Plain signatures (R||S) are exactly 64 bytes
        plainSignature.Should().HaveCount(64);
        _cryptoService.IsValidDerSignature(plainSignature).Should().BeFalse(); // It's not DER

        isValid.Should().BeTrue("The plain signature should be valid for the given data and public key.");

        // Optional: Verify with incorrect public key fails
        // Derive a different key to test against
         var otherKeyPair = _keyGenerationService.DeriveKeyFromSeed(seedHex, didIndex, keyType, keyIndex + 1, "other-key", curve);
        bool isInvalidPk = _cryptoService.VerifyDataSecp256k1WithoutDER(dataToSign, plainSignature, otherKeyPair.PublicKey.GetBytes());
        isInvalidPk.Should().BeFalse("Plain verification should fail with the wrong public key.");

        // Optional: Test DER conversion
        byte[] convertedDer = _cryptoService.ConvertToDerSignature(plainSignature);
        convertedDer.Length.Should().BeInRange(70, 72);
        _cryptoService.IsValidDerSignature(convertedDer).Should().BeTrue();
        bool isValidConverted = _cryptoService.VerifyDataSecp256k1(dataToSign, convertedDer, keyPair.PublicKey.GetBytes());
        isValidConverted.Should().BeTrue("Verification should succeed with DER signature converted from plain.");

        byte[] convertedPlain = _cryptoService.ConvertFromDerSignature(convertedDer);
        convertedPlain.Should().Equal(plainSignature);
        bool isValidReConverted = _cryptoService.VerifyDataSecp256k1WithoutDER(dataToSign, convertedPlain, keyPair.PublicKey.GetBytes());
        isValidReConverted.Should().BeTrue("Verification should succeed with plain signature converted back from DER.");
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