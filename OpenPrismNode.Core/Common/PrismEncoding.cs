namespace OpenPrismNode.Core.Common;

using System.Text;
using FluentResults;
using Google.Protobuf;

public static class PrismEncoding
{
    /// <summary>
    /// Converts a hexadecimal string to a byte array. Can throw exceptions if hex string is invalid.
    /// For a safe version that returns a Result, use TryHexToByteArray.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A byte array representing the hexadecimal string</returns>
    public static byte[] HexToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.ToLowerInvariant().Substring(x, 2), 16))
            .ToArray();
    }
    
    /// <summary>
    /// Safely tries to convert a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A Result containing the byte array if successful, or an error if conversion fails</returns>
    public static Result<byte[]> TryHexToByteArray(string hex)
    {
        // Check for null or empty input
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Result.Fail("Hexadecimal string cannot be empty.");
        }
        
        // Check if input contains only valid hex characters (0-9, a-f, A-F)
        if (!hex.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
        {
            return Result.Fail("Invalid hexadecimal string. Must contain only characters 0-9, a-f, or A-F.");
        }
        
        // Check if the length is correct (must be even for proper byte conversion)
        if (hex.Length % 2 != 0)
        {
            return Result.Fail("Invalid hexadecimal string. Must have an even length.");
        }

        try
        {
            var byteArray = Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.ToLowerInvariant().Substring(x, 2), 16))
                .ToArray();
                
            return Result.Ok(byteArray);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to parse hexadecimal string: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a hexadecimal string to a ByteString. Can throw exceptions if hex string is invalid.
    /// For a safe version that returns a Result, use TryHexToByteString.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A ByteString representing the hexadecimal string</returns>
    public static ByteString HexToByteString(string hex)
    {
        return ByteArrayToByteString(HexToByteArray(hex));
    }
    
    /// <summary>
    /// Safely tries to convert a hexadecimal string to a ByteString.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A Result containing the ByteString if successful, or an error if conversion fails</returns>
    public static Result<ByteString> TryHexToByteString(string hex)
    {
        var byteArrayResult = TryHexToByteArray(hex);
        if (byteArrayResult.IsFailed)
        {
            return Result.Fail(byteArrayResult.Errors);
        }
        
        return Result.Ok(ByteArrayToByteString(byteArrayResult.Value));
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
            case 2:
                s += "==";
                break; // Two pad chars
            case 3:
                s += "=";
                break; // One pad char
            default:
                throw new Exception(
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
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Result.Fail("Hexadecimal string cannot be empty.");
        }
        
        // Check if input contains only valid hex characters (0-9, a-f, A-F)
        if (!hex.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
        {
            return Result.Fail("Invalid hexadecimal string. Must contain only characters 0-9, a-f, or A-F.");
        }
        
        if (!hex.StartsWith("04"))
        {
            return Result.Fail("Invalid public key format. Must start with '04'.");
        }
        
        hex = hex.Substring(2, hex.Length - 2);
        var numberChars = hex.Length;
        
        // Validate the length after removing the prefix
        if (numberChars % 2 != 0)
        {
            return Result.Fail("Invalid hexadecimal string. Must have an even length.");
        }
        
        // if (numberChars < 128) // We need at least 64 bytes (128 hex chars) for x and y
        // {
        //     return Result.Fail("Invalid public key length. Not enough data for X and Y coordinates.");
        // }
        
        try
        {
            var hexAsBytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            var x = hexAsBytes.Take(32).ToArray();
            var y = hexAsBytes.Skip(32).Take(32).ToArray();

            return Result.Ok((x, y));
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to parse public key: {ex.Message}");
        }
    }

    public static string PublicKeyPairByteArraysToHex(byte[] x, byte[]? y)
    {
        var sb = new StringBuilder();
        sb.Append("04");
        sb.Append(ByteArrayToHex(x));
        if (y is not null)
        {
            sb.Append(ByteArrayToHex(y));
        }

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
    
    public static string ByteStringToUtf8String(ByteString byteString)
    {
        return Encoding.UTF8.GetString(byteString.ToByteArray());
    }

    public static string ByteArrayToUtf8String(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    public static string ByteStringToHex(ByteString byteString)
    {
        return ByteArrayToHex(byteString.ToByteArray());
    }

    public static string Utf8StringToHex(string utf8String)
    {
        var bytes = Utf8StringToByteArray(utf8String);
        return ByteArrayToHex(bytes);
    }

    /// <summary>
    /// Converts a hexadecimal string to a UTF-8 string. Can throw exceptions if hex string is invalid.
    /// For a safe version that returns a Result, use TryHexToUtf8String.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A UTF-8 string representing the hexadecimal string</returns>
    public static string HexToUtf8String(string hex)
    {
        var bytes = HexToByteArray(hex);
        return ByteArrayToUtf8String(bytes);
    }
    
    /// <summary>
    /// Safely tries to convert a hexadecimal string to a UTF-8 string.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A Result containing the UTF-8 string if successful, or an error if conversion fails</returns>
    public static Result<string> TryHexToUtf8String(string hex)
    {
        var byteArrayResult = TryHexToByteArray(hex);
        if (byteArrayResult.IsFailed)
        {
            return Result.Fail(byteArrayResult.Errors);
        }
        
        try
        {
            var utf8String = ByteArrayToUtf8String(byteArrayResult.Value);
            return Result.Ok(utf8String);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to convert bytes to UTF-8 string: {ex.Message}");
        }
    }

    public static string Utf8StringToBase64(string utf8String)
    {
        var bytes = Utf8StringToByteArray(utf8String);
        return ByteArrayToBase64(bytes);
    }

    public static string ByteStringToBase64(ByteString byteString)
    {
        return ByteArrayToBase64(byteString.ToByteArray());
    }

    public static ByteString Base64ToByteString(string base64String)
    {
        var bytes = Base64ToByteArray(base64String);
        return ByteString.CopyFrom(bytes);
    }

    public static string Base64ToHex(string base64String)
    {
        var bytes = Base64ToByteArray(base64String);
        return ByteArrayToHex(bytes);
    }

    /// <summary>
    /// Converts a hexadecimal string to a Base64 string. Can throw exceptions if hex string is invalid.
    /// For a safe version that returns a Result, use TryHexToBase64.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A Base64 string representing the hexadecimal string</returns>
    public static string HexToBase64(string hex)
    {
        var bytes = HexToByteArray(hex);
        return ByteArrayToBase64(bytes);
    }
    
    /// <summary>
    /// Safely tries to convert a hexadecimal string to a Base64 string.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>A Result containing the Base64 string if successful, or an error if conversion fails</returns>
    public static Result<string> TryHexToBase64(string hex)
    {
        var byteArrayResult = TryHexToByteArray(hex);
        if (byteArrayResult.IsFailed)
        {
            return Result.Fail(byteArrayResult.Errors);
        }
        
        try
        {
            var base64String = ByteArrayToBase64(byteArrayResult.Value);
            return Result.Ok(base64String);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to convert bytes to Base64 string: {ex.Message}");
        }
    }
}