namespace OpenPrismNode.Core.Services.Did;

using System.Text.Json.Serialization;
using EnsureThat;

public sealed class PrismPrivateKey
{
    [JsonConstructor]
    public PrismPrivateKey(byte[] privateKey)
    {
        Ensure.That(privateKey.Length).Is(32);
        PrivateKey = privateKey;
    }

    public byte[] PrivateKey { get; }
}