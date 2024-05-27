namespace OpenPrismNode.Core.Crypto;

public interface IEcService
{
    public byte[] SignData(byte[] dataToSign, byte[] privateKey);
    public bool VerifyData(byte[] dataToVerify, byte[] signature, byte[] publicKey);
    public byte[] SignDataWithoutDER(byte[] dataToSign, byte[] privateKey);
    public bool VerifyDataWithoutDER(byte[] dataToVerify, byte[] signature, byte[] publicKey);
    public bool CheckKeys(byte[] privateKey, byte[] publicKey);

    byte[] ConvertToDerSignature(byte[] signature);
    byte[] ConvertFromDerSignature(byte[] derSignature);
    public bool IsValidDerSignature(byte[] signature);
}