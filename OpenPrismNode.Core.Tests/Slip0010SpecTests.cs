using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Services.Did;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

public class Slip0010SpecTests
{
    private readonly ICryptoService _cryptoService;
    // We might instantiate the derivation service directly if needed for DeriveHardenedChild
    private readonly Slip0010DerivationServiceCardanoSharp _derivationService;

    public Slip0010SpecTests()
    {
        _cryptoService = new CryptoServiceBouncyCastle();
        _derivationService = new Slip0010DerivationServiceCardanoSharp(); // Use the custom implementation
    }

    // Helper method based on SLIP-0010 spec for master node generation
    private (byte[] MasterKey, byte[] MasterChainCode) GenerateMasterNodeEd25519(byte[] seed)
    {
        const string hmacKey = "ed25519 seed";
        using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hmacKey)))
        {
            byte[] i = hmac.ComputeHash(seed);
            byte[] il = i[..32]; // Master Key (k_m)
            byte[] ir = i[32..]; // Master Chain Code (c_m)
            return (il, ir);
        }
    }

    // Helper method to access the private derivation logic if needed
    // (Alternatively, make DeriveHardenedChild public/internal in the service)
    // For simplicity here, we re-implement the core logic needed.
     private (byte[] ChildKey, byte[] ChildChainCode) DeriveHardenedChild(byte[] parentKey, byte[] parentChainCode, uint hardenedIndex)
    {
        byte[] data = new byte[1 + 32 + 4];
        data[0] = 0x00;
        Buffer.BlockCopy(parentKey, 0, data, 1, 32);
        byte[] indexBytes = BitConverter.GetBytes(hardenedIndex);
        if (BitConverter.IsLittleEndian) Array.Reverse(indexBytes);
        Buffer.BlockCopy(indexBytes, 0, data, 1 + 32, 4);

        using (var hmac = new HMACSHA512(parentChainCode))
        {
            byte[] i = hmac.ComputeHash(data);
            byte[] childKey = i[..32];
            byte[] childChainCode = i[32..];
            return (childKey, childChainCode);
        }
    }


    [Fact]
    public void Slip0010_Ed25519_TestVector1_ShouldPass()
    {
        // Arrange
        // --- Test Vector 1 Values ---
        string seedHex = "000102030405060708090a0b0c0d0e0f";
        string expectedMasterKeyHex = "2b4be7f19ee27bbf30c667b642d5f4aa69fd169872f8fc3059c08ebae2eb19e7";
        string expectedMasterChainCodeHex = "90046a93de5380a72b5e45010748567d5ea02bbf6522f979e05c0d8d8ca9fffb";
        string path = "m/0'"; // Only the first hardened derivation step
        uint index0H = 0 | 0x80000000;
        string expectedChildKeyHex = "68e0fe46dfb67e368c75379acec591dad19df3cde26e63b93a8e704f1dade7a3";
        // Public key from spec has a 00 prefix, BouncyCastle returns raw 32 bytes
        string expectedChildPublicKeyHexWithPrefix = "008c8a13df77a28f3445213a0f432fde644acaa215fc72dcdf300d5efaa85d350c";
        string expectedChildPublicKeyHex = expectedChildPublicKeyHexWithPrefix.Substring(2); // Remove "00" prefix
        // --- End Test Vector 1 Values ---

        byte[] seed = Convert.FromHexString(seedHex);
        byte[] expectedMasterKey = Convert.FromHexString(expectedMasterKeyHex);
        byte[] expectedMasterChainCode = Convert.FromHexString(expectedMasterChainCodeHex);
        byte[] expectedChildKey = Convert.FromHexString(expectedChildKeyHex);
        byte[] expectedChildPublicKey = Convert.FromHexString(expectedChildPublicKeyHex);

        byte[] dataToSign = Encoding.UTF8.GetBytes("Test message for SLIP-0010 Vector 1");

        // Act

        // 1. Generate Master Node
        (byte[] masterKey, byte[] masterChainCode) = GenerateMasterNodeEd25519(seed);

        // 2. Verify Master Node
        masterKey.Should().Equal(expectedMasterKey, "Master key should match test vector");
        masterChainCode.Should().Equal(expectedMasterChainCode, "Master chain code should match test vector");

        // 3. Derive Child Node (m/0') using the derived master node
        (byte[] derivedChildKey, byte[] derivedChildChainCode) = DeriveHardenedChild(masterKey, masterChainCode, index0H);

        // 4. Verify Child Private Key Seed
        derivedChildKey.Should().Equal(expectedChildKey, "Derived child key seed (m/0') should match test vector");

        // 5. Derive Child Public Key using BouncyCastle from the *expected* child key seed
        byte[] derivedPublicKey = _cryptoService.GetEd25519PublicKeyFromPrivateKey(expectedChildKey);

        // 6. Verify Child Public Key
        derivedPublicKey.Should().Equal(expectedChildPublicKey, "BouncyCastle derived public key should match test vector (without prefix)");

        // 7. Sign using the *expected* child key seed
        byte[] signature = _cryptoService.SignDataEd25519(dataToSign, expectedChildKey);
        signature.Should().NotBeNull().And.HaveCount(64);

        // 8. Verify Signature using the *expected* public key
        bool isValid = _cryptoService.VerifyDataEd25519(dataToSign, signature, expectedChildPublicKey);

        // Assert
        isValid.Should().BeTrue("Signature verification should succeed for SLIP-0010 Test Vector 1");

        // Optional: Verify using the derived keys as well (should be identical)
         bool isValidUsingDerived = _cryptoService.VerifyDataEd25519(dataToSign, signature, derivedPublicKey);
         isValidUsingDerived.Should().BeTrue("Signature verification should also succeed using the derived public key");
    }

     [Fact]
    public void BouncyCastle_Ed25519_Direct_SignVerify_WithSpecKeys_ShouldPass_Explicit()
    {
        // Arrange
        // Keys directly from SLIP-0010 Test Vector 1, m/0'
        string childKeySeedHex = "68e0fe46dfb67e368c75379acec591dad19df3cde26e63b93a8e704f1dade7a3";
        string childPublicKeyHex = "8c8a13df77a28f3445213a0f432fde644acaa215fc72dcdf300d5efaa85d350c"; // Without 00 prefix

        byte[] privateKeySeed = Convert.FromHexString(childKeySeedHex); // This is k_c
        byte[] expectedPublicKey = Convert.FromHexString(childPublicKeyHex); // This is A_c
        byte[] dataToSign = Encoding.UTF8.GetBytes("Direct BouncyCastle Test");

        // Act

        // 1. Verify public key derivation using BouncyCastle directly
        var privParamsForPubGen = new Ed25519PrivateKeyParameters(privateKeySeed, 0);
        var pubParamsDerived = privParamsForPubGen.GeneratePublicKey();
        byte[] derivedPkBytes = pubParamsDerived.GetEncoded();
        derivedPkBytes.Should().Equal(expectedPublicKey, "BC should derive the correct public key from the private seed k_c");

        // 2. Sign using BouncyCastle directly
        var privParamsForSign = new Ed25519PrivateKeyParameters(privateKeySeed, 0); // Use k_c
        var signer = new Ed25519Signer();
        signer.Reset();
        signer.Init(true, privParamsForSign); // Init with k_c
        signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
        byte[] signature = signer.GenerateSignature();
        signature.Should().NotBeNull().And.HaveCount(64);

        // 3. Verify using BouncyCastle directly
        var pubParamsForVerify = new Ed25519PublicKeyParameters(expectedPublicKey, 0); // Use A_c
        var verifier = new Ed25519Signer();
        verifier.Reset();
        verifier.Init(false, pubParamsForVerify); // Init with A_c
        verifier.BlockUpdate(dataToSign, 0, dataToSign.Length);
        bool isValid = false;
        Exception? verifyEx = null;
        try
        {
             isValid = verifier.VerifySignature(signature);
        }
        catch(Exception ex)
        {
            verifyEx = ex;
        }

        // Assert
        verifyEx.Should().BeNull("Verification should not throw an exception.");
        isValid.Should().BeTrue("Direct BouncyCastle sign/verify with k_c and A_c from SLIP-0010 spec should succeed");
    }
}

