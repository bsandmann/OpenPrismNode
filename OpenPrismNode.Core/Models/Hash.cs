namespace OpenPrismNode.Core.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Crypto;

// [JsonConverter(typeof(HashJsonConverter))]
public sealed class Hash
{
// #pragma warning disable CS8618
     private static ISha256Service _sha256Service;
// #pragma warning restore CS8618

    public byte[] Value { get; private set; }

    private const byte LeafPrefix = 0;
    private const byte NodePrefix = 1;

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
    //
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
    //
    // public Hash Prefix(ISha256Service sha256Service)
    // {
    //     return new Hash(sha256Service).Of(PrefixByteArray(LeafPrefix, Value));
    // }
    //
    // public Hash Combine(Hash left, Hash right, ISha256Service sha256Service)
    // {
    //     return new Hash(sha256Service).Of(PrefixByteArray(NodePrefix, CombineByteArrays(left.Value, right.Value)));
    // }
    //
    // public string AsHex()
    // {
    //     return PrismEncoding.ByteArrayToHex(this.Value);
    // }
    //
    // private static byte[] CombineByteArrays(byte[] left, byte[] right)
    // {
    //     byte[] newArray = new byte[left.Length + right.Length];
    //     left.CopyTo(newArray, 0);
    //     right.CopyTo(newArray, left.Length);
    //     return newArray;
    // }
    //
    // private static byte[] PrefixByteArray(byte newByte, byte[] bArray)
    // {
    //     byte[] newArray = new byte[bArray.Length + 1];
    //     bArray.CopyTo(newArray, 1);
    //     newArray[0] = newByte;
    //     return newArray;
    // }
}