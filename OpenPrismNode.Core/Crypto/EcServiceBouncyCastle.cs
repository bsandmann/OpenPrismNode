namespace OpenPrismNode.Core.Crypto
{
    using EnsureThat;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Sec;
    using Org.BouncyCastle.Asn1.X9;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Security;

    /// <summary>
    /// Cryptographic Methods to sign and verify data based on ECDSA and SHA256
    /// </summary>
    public sealed class EcServiceBouncyCastle : IEcService
    {
        private X9ECParameters Curve { get; }
        private ISigner SignerAndVerifier { get; }
        private ISigner SignerAndVerifierWithoutDER { get; }
        private ECDomainParameters EcDomainParameters { get; }

        public EcServiceBouncyCastle()
        {
            Curve = SecNamedCurves.GetByName(PrismParameters.Secp256k1CurveName);
            SignerAndVerifier = SignerUtilities.GetSigner("SHA-256withECDSA");
            SignerAndVerifierWithoutDER = SignerUtilities.GetSigner("SHA-256withPLAIN-ECDSA");
            EcDomainParameters = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H, Curve.GetSeed());
        }

        private const int KeyLength = 32;
        private const int PublicKeyLength = 65;

        /// <summary>
        /// Sign data
        /// </summary>
        /// <param name="dataToSign"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public byte[] SignData(byte[] dataToSign, byte[] privateKey)
        {
            return Sign(dataToSign, privateKey, SignerAndVerifier);
        }

        /// <summary>
        /// Sign data
        /// </summary>
        /// <param name="dataToSign"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public byte[] SignDataWithoutDER(byte[] dataToSign, byte[] privateKey)
        {
            return Sign(dataToSign, privateKey, SignerAndVerifierWithoutDER);
        }

        private byte[] Sign(byte[] dataToSign, byte[] privateKey, ISigner signer)
        {
            Ensure.That(privateKey.Length).Is(KeyLength);
            var bigInteger = new BigInteger(1, privateKey);
            var privateKeyParameters = new ECPrivateKeyParameters(bigInteger, EcDomainParameters);
            signer.Init(true, privateKeyParameters);
            signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
            var signature = signer.GenerateSignature();
            return signature;
        }

        public byte[] ConvertToDerSignature(byte[] signature)
        {
            Ensure.That(signature.Length).Is(PublicKeyLength);

            // Assuming signature is 64 bytes: 32 bytes for 'r' and 32 bytes for 's'
            var rBytes = new byte[KeyLength];
            var sBytes = new byte[KeyLength];
            Array.Copy(signature, 0, rBytes, 0, KeyLength);
            Array.Copy(signature, KeyLength, sBytes, 0, KeyLength);

            var r = new BigInteger(1, rBytes);
            var s = new BigInteger(1, sBytes);

            var v = new Asn1EncodableVector
            {
                new DerInteger(r),
                new DerInteger(s)
            };

            var seq = new DerSequence(v);
            var derEncodedSignature = seq.GetDerEncoded();

            return derEncodedSignature;
        }

        public byte[] ConvertFromDerSignature(byte[] derSignature)
        {
            Ensure.That(derSignature.Length).IsGte(70);
            Ensure.That(derSignature.Length).IsLte(72);

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(derSignature);

            var r = ((DerInteger)seq[0]).Value;
            var s = ((DerInteger)seq[1]).Value;

            var rBytes = r.ToByteArrayUnsigned(); // get byte array and ensure it is unsigned for correct padding
            var sBytes = s.ToByteArrayUnsigned(); // get byte array and ensure it is unsigned for correct padding

            // Ensure both r and s are 32 bytes long by adding zero padding if required
            if (rBytes.Length < KeyLength)
            {
                rBytes = new byte[KeyLength - rBytes.Length].Concat(rBytes).ToArray();
            }

            if (sBytes.Length < KeyLength)
            {
                sBytes = new byte[KeyLength - sBytes.Length].Concat(sBytes).ToArray();
            }

            // Concatenate r and s bytes
            byte[] signature = rBytes.Concat(sBytes).ToArray();

            return signature;
        }

        public bool IsValidDerSignature(byte[] signature)
        {
            try
            {
                // Attempt to parse the signature as a DER sequence.
                var seq = (Asn1Sequence)Asn1Object.FromByteArray(signature);

                // A valid ECDSA signature should have exactly two components.
                if (seq.Count != 2)
                {
                    return false;
                }

                // Both components should be integers.
                if (!(seq[0] is DerInteger) || !(seq[1] is DerInteger))
                {
                    return false;
                }

                return true;
            }
            catch (IOException)
            {
                // If the signature can't be parsed as a DER sequence, it's not a valid DER signature.
                return false;
            }
        }


        /// <summary>
        /// Verify that the signature is a valid signature for the original data
        /// </summary>
        /// <param name="dataToVerify">unsigned data</param>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool VerifyData(byte[] dataToVerify, byte[] signature, byte[] publicKey)
        {
            ECPublicKeyParameters publicKeyParameters;
            try
            {
                Ensure.That(publicKey.Length).Is(PublicKeyLength);
                publicKeyParameters = new ECPublicKeyParameters("EC", Curve.Curve.DecodePoint(publicKey), EcDomainParameters);
            }
            catch (Exception)
            {
                // this gets thrown if the point (the publicKey) is invalid (e.g. just some random 65-byte-long-array)
                return false;
            }

            SignerAndVerifier.Init(false, publicKeyParameters);
            SignerAndVerifier.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
            return SignerAndVerifier.VerifySignature(signature);
        }

        /// <summary>
        /// Verify that the signature is a valid signature for the original data
        /// </summary>
        /// <param name="dataToVerify">unsigned data</param>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool VerifyDataWithoutDER(byte[] dataToVerify, byte[] signature, byte[] publicKey)
        {
            ECPublicKeyParameters publicKeyParameters;
            try
            {
                Ensure.That(publicKey.Length).Is(PublicKeyLength);
                publicKeyParameters = new ECPublicKeyParameters("EC", Curve.Curve.DecodePoint(publicKey), EcDomainParameters);
            }
            catch (Exception)
            {
                // this gets thrown if the point (the publicKey) is invalid (e.g. just some random 65-byte-long-array)
                return false;
            }

            SignerAndVerifierWithoutDER.Init(false, publicKeyParameters);
            SignerAndVerifierWithoutDER.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
            var verificationResult = SignerAndVerifierWithoutDER.VerifySignature(signature);
            return verificationResult;
        }

        public bool CheckKeys(byte[] privateKey, byte[] publicKey)
        {
            var someData = privateKey;
            var curve = SecNamedCurves.GetByName(PrismParameters.Secp256k1CurveName);
            var signerAndVerifier = SignerUtilities.GetSigner("SHA-256withECDSA");
            var ecDomainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
            BigInteger bigInteger = new BigInteger(1, privateKey);
            var privateKeyParameters = new ECPrivateKeyParameters(bigInteger, ecDomainParameters);
            signerAndVerifier.Init(true, privateKeyParameters);
            signerAndVerifier.BlockUpdate(someData, 0, someData.Length);
            var signature = signerAndVerifier.GenerateSignature();
            ECPublicKeyParameters publicKeyParameters = new ECPublicKeyParameters("EC", curve.Curve.DecodePoint(publicKey), ecDomainParameters);
            signerAndVerifier.Init(false, publicKeyParameters);
            signerAndVerifier.BlockUpdate(someData, 0, someData.Length);
            var verificationResult = signerAndVerifier.VerifySignature(signature);
            return verificationResult;
        }
    }
}