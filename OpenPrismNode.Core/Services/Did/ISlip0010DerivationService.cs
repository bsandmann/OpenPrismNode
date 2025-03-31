namespace OpenPrismNode.Core.Services.Did;

public interface ISlip0010DerivationService
{
    /// <summary>
    /// Derives an Ed25519 private key seed (32 bytes) for a given path from a master seed.
    /// </summary>
    /// <param name="seedHex">The master seed as a hex string (typically 64 bytes).</param>
    /// <param name="path">The BIP32/SLIP-0010 derivation path (e.g., "m/0'/3'/1'").</param>
    /// <returns>The 32-byte private key seed (k).</returns>
    /// <exception cref="ArgumentException">Thrown if seed or path is invalid.</exception>
    byte[] DerivePrivateKey(string seedHex, string path);

    /// <summary>
    /// Derives an Ed25519 private key seed (32 bytes) for a given path from master seed bytes.
    /// </summary>
    /// <param name="seedBytes">The master seed bytes (typically 64 bytes).</param>
    /// <param name="path">The BIP32/SLIP-0010 derivation path.</param>
    /// <returns>The 32-byte private key seed (k).</returns>
    /// <exception cref="ArgumentException">Thrown if seed or path is invalid.</exception>
    byte[] DerivePrivateKey(byte[] seedBytes, string path);
}