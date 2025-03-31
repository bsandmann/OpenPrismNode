namespace OpenPrismNode.Core.Crypto;

public interface ICryptoService
{
    // Secp256k1
    byte[] SignDataSecp256k1(byte[] dataToSign, byte[] privateKey);
    bool VerifyDataSecp256k1(byte[] dataToVerify, byte[] signature, byte[] publicKeyUncompressed); // Expect 65 bytes
    byte[] SignDataSecp256k1WithoutDER(byte[] dataToSign, byte[] privateKey);
    bool VerifyDataSecp256k1WithoutDER(byte[] dataToVerify, byte[] signature, byte[] publicKeyUncompressed); // Expect 65 bytes
    byte[] ConvertToDerSignature(byte[] plainSignature); // Input 64 bytes r+s
    byte[] ConvertFromDerSignature(byte[] derSignature); // Output 64 bytes r+s
    bool IsValidDerSignature(byte[] signature);

    // Ed25519
    byte[] SignDataEd25519(byte[] dataToSign, byte[] privateKey); // Expect 32 bytes
    bool VerifyDataEd25519(byte[] dataToVerify, byte[] signature, byte[] publicKey); // Expect 32 bytes

    byte[] GetEd25519PublicKeyFromPrivateKey(byte[] ed25519PrivateKey);

    // X25519
    byte[] GenerateSharedSecretX25519(byte[] privateKey, byte[] peerPublicKey); // Both 32 bytes
    byte[] ConvertEd25519PrivateKeyToX25519(byte[] ed25519PrivateKey); // Input 32 bytes
    byte[] GetX25519PublicKeyFromPrivateKey(byte[] x25519PrivateKey); // Input 32 bytes

    // Unified Key Check
    /// <summary>
    /// Checks if a private key corresponds to a public key for the given curve.
    /// </summary>
    /// <param name="privateKey">The private key bytes.</param>
    /// <param name="publicKey">The public key bytes in the standard format for the curve (Secp256k1: 65 bytes uncompressed, Ed/X25519: 32 bytes raw).</param>
    /// <param name="curve">The curve name (e.g., "secp256k1", "Ed25519", "X25519").</param>
    /// <returns>True if the keys match, false otherwise.</returns>
    bool CheckKeys(byte[] privateKey, byte[] publicKey, string curve);
}