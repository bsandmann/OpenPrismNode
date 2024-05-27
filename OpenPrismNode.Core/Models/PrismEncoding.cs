namespace OpenPrismNode.Core.Models;

using System.Text;
using FluentResults;
using Google.Protobuf;

public static class PrismEncoding
{
    public static byte[] HexToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.ToLowerInvariant().Substring(x, 2), 16))
            .ToArray();
    }

    public static ByteString HexToByteString(string hex)
    {
        return ByteArrayToByteString(HexToByteArray(hex));
    }

    public static byte[] Utf8StringToByteArray(string utf8String)
    {
        return Encoding.UTF8.GetBytes(utf8String);
    }

    public static ByteString Utf8StringToByteString(string utf8String)
    {
        return ByteString.CopyFrom(Utf8StringToByteArray(utf8String));
    }

    public static string ByteArrayToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static byte[] ByteArrayStringToByteArray(string byteArrayString)
    {
        var byteSplit = byteArrayString.Replace("[", "").Replace("]", "").Split(",");
        var byteArray = byteSplit.ToList().Select(p => (byte)sbyte.Parse(p.Trim())).ToArray();
        return byteArray;
    }

    public static ByteString ByteArrayToByteString(byte[] bytes)
    {
        return ByteString.CopyFrom(bytes);
    }

    public static byte[] ByteStringToByteArray(ByteString byteString)
    {
        return byteString.ToByteArray();
    }

    public static string ByteArrayToBase64(byte[] arg)
    {
        // URL Encoded version! required for prism
        string s = Convert.ToBase64String(arg); // Regular base64 encoder
        s = s.Split('=')[0]; // Remove any trailing '='s
        s = s.Replace('+', '-'); // 62nd char of encoding
        s = s.Replace('/', '_'); // 63rd char of encoding
        return s;
    }

    public static byte[] Base64ToByteArray(string base64String)
    {
        string s = base64String;
        s = s.Replace('-', '+'); // 62nd char of encoding
        s = s.Replace('_', '/'); // 63rd char of encoding
        switch (s.Length % 4) // Pad with trailing '='s
        {
            case 0: break; // No pad chars in this case
            case 2: s += "=="; break; // Two pad chars
            case 3: s += "="; break; // One pad char
            default: throw new Exception(
                "Illegal base64url string!");
        }
        return Convert.FromBase64String(s); // Standard base64 decoder
    }

    public static string Base64ToString(string base64String)
    {
        var bytes = Base64ToByteArray(base64String);
        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    public static Result<(byte[] x, byte[] y)> HexToPublicKeyPairByteArrays(string hex)
    {
        if (hex.StartsWith("04"))
        {
            hex = hex.Substring(2, hex.Length - 2);
            var numberChars = hex.Length;
            var hexAsBytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            var x = hexAsBytes.Take(32).ToArray();
            var y = hexAsBytes.Skip(32).Take(32).ToArray();
            return Result.Ok((x, y));
        }

        return Result.Fail("invalid form");
    }
    
    public static string PublicKeyPairByteArraysToHex(byte[] x, byte[] y)
    {
        var sb = new StringBuilder();
        sb.Append("04");
        sb.Append(ByteArrayToHex(x));
        sb.Append(ByteArrayToHex(y));
        return sb.ToString();
    }
    
    public static byte[] Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 0:
                return Convert.FromBase64String(s);
            case 2:
                s += "==";
                goto case 0;
            case 3:
                s += "=";
                goto case 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(input), "Illegal base64url string!");
        }
    }
}