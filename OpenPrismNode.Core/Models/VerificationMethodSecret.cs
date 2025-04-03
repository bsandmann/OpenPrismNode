namespace OpenPrismNode.Core.Models;

public class VerificationMethodSecret
{
    public VerificationMethodSecret(string prismKeyUsage, string keyId, string curve, byte[] bytes, bool isRemoveOperation, string? mnemonic)
    {
        PrismKeyUsage = prismKeyUsage;
        KeyId = keyId;
        Curve = curve;
        Bytes = bytes;
        IsRemoveOperation = isRemoveOperation;
        Mnemonic = mnemonic;
    }


    public string PrismKeyUsage { get; }
    public string KeyId { get; }
    public string Curve { get; }
    public byte[] Bytes { get; }
    public bool IsRemoveOperation { get; }
    public string? Mnemonic { get; }
}