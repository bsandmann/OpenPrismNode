namespace OpenPrismNode.Core.Models;

using System.Diagnostics;
using System.Security.Cryptography;
using Crypto;

public sealed class Hash
{
    // TODO refactor or remove the Hash implemantion?

    private static ISha256Service _sha256Service;

    public byte[] Value { get; private set; }

    private Hash(byte[] value)
    {
        Debug.Assert(value != null);
        Debug.Assert(value.Length > 0);
        Value = value;
    }

    /// <summary>
    /// Hashes the provided data and creates a new instance of this class
    /// </summary>
    public Hash(ISha256Service sha256Service)
    {
        Debug.Assert(sha256Service != null);
        _sha256Service = sha256Service;
    }

    //
    public Hash Of(byte[] value)
    {
        var hash = _sha256Service.HashData(value);
        Debug.Assert(hash != null);
        this.Value = hash;
        return this;
    }

    /// <summary>
    /// Takes the provided data and wrappes it in this hash-class without hashing it
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Hash CreateFrom(byte[] value)
    {
        // Ensure.That(value.Length).Is(32);
        return new Hash(value);
    }

    public static Hash CreateRandom()
    {
        return new Hash(RandomNumberGenerator.GetBytes(32));
    }
}