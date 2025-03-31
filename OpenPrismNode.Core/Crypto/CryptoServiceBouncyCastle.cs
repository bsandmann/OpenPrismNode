namespace OpenPrismNode.Core.Crypto;

using EnsureThat;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.IO; // For IsValidDerSignature IOException
using System.Linq;

public sealed class CryptoServiceBouncyCastle : ICryptoService
{
    private const int Secp256k1PrivateKeyLength = 32;
    private const int Secp256k1PublicKeyLength = 65; // Uncompressed
    private const int Ed25519KeyLength = 32;
    private const int X25519KeyLength = 32;
    private const int PlainEcdsaSignatureLength = 64; // r + s

    private readonly X9ECParameters _secp256k1CurveParams;
    private readonly ECDomainParameters _secp256k1DomainParams;

    public CryptoServiceBouncyCastle()
    {
        _secp256k1CurveParams = SecNamedCurves.GetByName(PrismParameters.Secp256k1CurveName);
        _secp256k1DomainParams = new ECDomainParameters(
            _secp256k1CurveParams.Curve,
            _secp256k1CurveParams.G,
            _secp256k1CurveParams.N,
            _secp256k1CurveParams.H,
            _secp256k1CurveParams.GetSeed());
    }

    // --- Secp256k1 Methods ---

    public byte[] SignDataSecp256k1(byte[] dataToSign, byte[] privateKey)
    {
        return SignSecp256k1(dataToSign, privateKey, SignerUtilities.GetSigner("SHA-256withECDSA"));
    }

    public bool VerifyDataSecp256k1(byte[] dataToVerify, byte[] signature, byte[] publicKeyUncompressed)
    {
        return VerifySecp256k1(dataToVerify, signature, publicKeyUncompressed, SignerUtilities.GetSigner("SHA-256withECDSA"));
    }

    public byte[] SignDataSecp256k1WithoutDER(byte[] dataToSign, byte[] privateKey)
    {
        return SignSecp256k1(dataToSign, privateKey, SignerUtilities.GetSigner("SHA-256withPLAIN-ECDSA"));
    }

    public bool VerifyDataSecp256k1WithoutDER(byte[] dataToVerify, byte[] signature, byte[] publicKeyUncompressed)
    {
        return VerifySecp256k1(dataToVerify, signature, publicKeyUncompressed, SignerUtilities.GetSigner("SHA-256withPLAIN-ECDSA"));
    }

    private byte[] SignSecp256k1(byte[] dataToSign, byte[] privateKey, ISigner signer)
    {
        var keyParameters = new ECPrivateKeyParameters(new BigInteger(1, privateKey), _secp256k1DomainParams);
        signer.Init(true, keyParameters);
        signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
        return signer.GenerateSignature();
    }

    private bool VerifySecp256k1(byte[] dataToVerify, byte[] signature, byte[] publicKeyUncompressed, ISigner verifier)
    {
        if (publicKeyUncompressed[0] != 0x04) // Check for uncompressed format prefix
        {
            // Log or throw: Invalid public key format
            return false;
        }

        ECPublicKeyParameters publicKeyParameters;
        try
        {
            ECPoint q = _secp256k1CurveParams.Curve.DecodePoint(publicKeyUncompressed);
            publicKeyParameters = new ECPublicKeyParameters("EC", q, _secp256k1DomainParams);
        }
        catch (Exception) // Catches invalid point decoding
        {
            return false;
        }

        verifier.Init(false, publicKeyParameters);
        verifier.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
        return verifier.VerifySignature(signature);
    }

    public byte[] ConvertToDerSignature(byte[] plainSignature)
    {
        var rBytes = plainSignature.Take(Secp256k1PrivateKeyLength).ToArray();
        var sBytes = plainSignature.Skip(Secp256k1PrivateKeyLength).Take(Secp256k1PrivateKeyLength).ToArray();

        var r = new BigInteger(1, rBytes);
        var s = new BigInteger(1, sBytes);

        // Add S normalization check (low-S) if required by consumers, common in blockchain contexts
        // if (s.CompareTo(_secp256k1DomainParams.N.ShiftRight(1)) > 0) {
        //     s = _secp256k1DomainParams.N.Subtract(s);
        // }

        var v = new Asn1EncodableVector { new DerInteger(r), new DerInteger(s) };
        return new DerSequence(v).GetDerEncoded();
    }

    public byte[] ConvertFromDerSignature(byte[] derSignature)
    {
        try
        {
            var seq = Asn1Sequence.GetInstance(derSignature);
            if (seq.Count != 2 || !(seq[0] is DerInteger) || !(seq[1] is DerInteger))
            {
                throw new ArgumentException("Invalid DER signature format.", nameof(derSignature));
            }

            var r = ((DerInteger)seq[0]).Value;
            var s = ((DerInteger)seq[1]).Value;

            var rBytes = r.ToByteArrayUnsigned();
            var sBytes = s.ToByteArrayUnsigned();

            // Pad to 32 bytes
            var plainSig = new byte[PlainEcdsaSignatureLength];
            Array.Copy(rBytes, 0, plainSig, Secp256k1PrivateKeyLength - rBytes.Length, rBytes.Length);
            Array.Copy(sBytes, 0, plainSig, PlainEcdsaSignatureLength - sBytes.Length, sBytes.Length);

            return plainSig;
        }
        catch (Exception ex) // Catches ASN.1 parsing errors
        {
            throw new ArgumentException("Failed to parse DER signature.", nameof(derSignature), ex);
        }
    }

    public bool IsValidDerSignature(byte[] signature)
    {
        try
        {
            var seq = Asn1Sequence.GetInstance(signature);
            return seq.Count == 2 && seq[0] is DerInteger && seq[1] is DerInteger;
        }
        catch (IOException) { return false; } // BouncyCastle often throws IOException for parse errors
        catch (Exception) { return false; } // Catch other potential issues
    }

    // --- Ed25519 Methods ---

    public byte[] SignDataEd25519(byte[] dataToSign, byte[] privateKey)
    {
        var keyParameters = new Ed25519PrivateKeyParameters(privateKey, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, keyParameters);
        signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
        return signer.GenerateSignature();
    }

    public bool VerifyDataEd25519(byte[] dataToVerify, byte[] signature, byte[] publicKey)
    {
        var keyParameters = new Ed25519PublicKeyParameters(publicKey, 0);
        var verifier = new Ed25519Signer();
        verifier.Init(false, keyParameters);
        verifier.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
        return verifier.VerifySignature(signature);
    }

    public byte[] GetEd25519PublicKeyFromPrivateKey(byte[] ed25519PrivateKey)
    {
        var keyParameters = new Ed25519PrivateKeyParameters(ed25519PrivateKey, 0);
        var publicKeyParameters = keyParameters.GeneratePublicKey();
        return publicKeyParameters.GetEncoded(); // Returns the 32-byte public key
    }

    // --- X25519 Methods ---

    public byte[] GenerateSharedSecretX25519(byte[] privateKey, byte[] peerPublicKey)
    {
        var privParams = new X25519PrivateKeyParameters(privateKey, 0);
        var pubParams = new X25519PublicKeyParameters(peerPublicKey, 0);
        var agreement = new X25519Agreement();

        agreement.Init(privParams);
        byte[] sharedSecret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(pubParams, sharedSecret, 0);
        return sharedSecret;
    }

    /// <summary>
    /// Converts an Ed25519 private key (32-byte seed) to an X25519 private key
    /// following RFC 7748 conversion (hash and clamp).
    /// </summary>
    public byte[] ConvertEd25519PrivateKeyToX25519(byte[] ed25519PrivateKey)
    {
        // RFC 8032 defines Ed25519 private key as a 32-byte seed 'k'.
        // RFC 7748 defines X25519 key conversion from Ed25519 *signing key* (which is derived from 'k').
        // However, common practice and libraries often refer to converting the Ed25519 *seed* 'k'.
        // Let's assume the input is the 32-byte Ed25519 seed 'k'.
        // The conversion process typically involves hashing this seed.

        // Step 1: Hash the Ed25519 private key seed using SHA-512
        var digest = new Sha512Digest();
        digest.BlockUpdate(ed25519PrivateKey, 0, Ed25519KeyLength);
        byte[] hash = new byte[digest.GetDigestSize()]; // 64 bytes
        digest.DoFinal(hash, 0);

        // Step 2: Take the lower 32 bytes of the hash as the X25519 private key scalar
        byte[] x25519PrivateKey = hash[..X25519KeyLength];

        // Step 3: Apply clamping (as per RFC 7748)
        x25519PrivateKey[0] &= 248;  // 11111000
        x25519PrivateKey[31] &= 127; // 01111111
        x25519PrivateKey[31] |= 64;  // 01000000

        return x25519PrivateKey;
    }

    public byte[] GetX25519PublicKeyFromPrivateKey(byte[] x25519PrivateKey)
    {
        // Note: BouncyCastle expects the *clamped* private key here.
        var keyParameters = new X25519PrivateKeyParameters(x25519PrivateKey, 0);
        var publicKeyParameters = keyParameters.GeneratePublicKey();
        return publicKeyParameters.GetEncoded();
    }


    // --- Unified Key Check ---

    public bool CheckKeys(byte[] privateKey, byte[] publicKey, string curve)
    {
        try
        {
            switch (curve)
            {
                case PrismParameters.Secp256k1CurveName:
                    // Derive public key from private key and compare
                    var privInt = new BigInteger(1, privateKey);
                    ECPoint q = _secp256k1DomainParams.G.Multiply(privInt);
                    byte[] derivedPublicKey = q.GetEncoded(false); // false = uncompressed
                    return publicKey.SequenceEqual(derivedPublicKey);

                case PrismParameters.Ed25519CurveName:
                    // Derive public key from private key using BouncyCastle and compare
                    // This is the same logic as GetEd25519PublicKeyFromPrivateKey
                    var edPrivParams = new Ed25519PrivateKeyParameters(privateKey, 0);
                    var edPubParams = edPrivParams.GeneratePublicKey();
                    // Compare the provided publicKey with the BC-derived one
                    return publicKey.SequenceEqual(edPubParams.GetEncoded());

                case PrismParameters.X25519CurveName:
                    // Derive public key from private key and compare
                    // Important: X25519 check uses the *clamped* private key.
                    // The input `privateKey` here should ideally be the clamped one if generated via conversion.
                    // If it's the raw Ed25519 seed, the check might fail unless we clamp it here.
                    // Let's assume the input `privateKey` IS the correct X25519 private key.
                    var xPrivParams = new X25519PrivateKeyParameters(privateKey, 0);
                    var xPubParams = xPrivParams.GeneratePublicKey();
                    return publicKey.SequenceEqual(xPubParams.GetEncoded());

                default:
                    // Log unsupported curve
                    return false;
            }
        }
        catch (Exception ex) // Catch Ensure exceptions, BC exceptions, etc.
        {
            // Log exception details (ex.ToString())
            Console.WriteLine($"Error during key check for curve {curve}: {ex.Message}");
            return false;
        }
    }
}