namespace OpenPrismNode.Core.Crypto;

using EnsureThat;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Wrapper for the Hash-Method of Bouncy-Castle
/// </summary>
public sealed class Sha256ServiceBouncyCastle : ISha256Service
{
   /// <summary>
   /// Sha256 Hash of any byte encoded data
   /// </summary>
   /// <param name="encData"></param>
   /// <returns></returns>
   public byte[] HashData(byte[] encData)
   {
      Ensure.That(encData).IsNotNull();
      Ensure.That(encData.Length).IsNot(0);
      
      var hash = new Org.BouncyCastle.Crypto.Digests.Sha256Digest();
      hash.BlockUpdate (encData, 0, encData.Length);
      var compArr = new byte[hash.GetDigestSize ()]; 
      hash.DoFinal (compArr, 0);
      return compArr;
   }
}