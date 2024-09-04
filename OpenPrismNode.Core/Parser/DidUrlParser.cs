namespace OpenPrismNode.Core.Parser;

using System.Text.RegularExpressions;

public static class DidUrlParser
{
    /// <summary>
    /// This code is crap! TODO rewrite.
    /// </summary>
    /// <param name="didUrl"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string didUrl, out ParsedDidUrl result)
    {
        result = null!;
        if (string.IsNullOrEmpty(didUrl))
        {
            return false;
        }

        // Main parsing logic
        var didMatch = Regex.Match(didUrl, @"^did:(\w+):([\w.%-]+)(.*)");
        if (!didMatch.Success) return false;

        var methodName = didMatch.Groups[1].Value;
        var methodSpecificId = didMatch.Groups[2].Value;
        var remainder = didMatch.Groups[3].Value;
        var network = string.Empty;
        var subNetwork = string.Empty;
        if (remainder.StartsWith(':'))
        {
            network = didMatch.Groups[2].Value;
            var remainderMatch = Regex.Match(didMatch.Groups[3].Value, @"^(.*?)([/\?#].*)?$");
            if (remainderMatch.Groups[1].Value.Split(":").Length == 3)
            {
                subNetwork = remainderMatch.Groups[1].Value.Split(":")[1];
                methodSpecificId = remainderMatch.Groups[1].Value.Replace($":{subNetwork}:", "");
                remainder = remainderMatch.Groups[2].Value;
            }
            else
            {
                methodSpecificId = remainderMatch.Groups[1].Value.Replace(":", "");
                remainder = remainderMatch.Groups[2].Value;
            }
        }

        string path = "", query = "", fragment = "";

        // Extract path, query, and fragment
        var uriMatch = Regex.Match(remainder, @"^(/[^?#]*)?(\?[^#]*)?(#.*)?");
        if (uriMatch.Success)
        {
            path = uriMatch.Groups[1].Value;
            query = uriMatch.Groups[2].Value.StartsWith("?") ? uriMatch.Groups[2].Value.Substring(1) : ""; // Removing '?' from the start
            fragment = uriMatch.Groups[3].Value.StartsWith("#") ? uriMatch.Groups[3].Value.Substring(1) : ""; // Removing '#' from the start
        }

        // Validate method name according to the ruleset (only lowercase letters and digits)
        if (!Regex.IsMatch(methodName, @"^[a-z0-9]+$"))
        {
            return false;
        }

        // Validate method-specific-id. It should not be empty, and should comply with the ruleset
        if (string.IsNullOrWhiteSpace(methodSpecificId) || !Regex.IsMatch(methodSpecificId, @"^[\w.-]+(:[\w.-]+)*$"))
        {
            return false;
        }

        // Validate path, query, and fragment. These can be empty but if present, they should comply with the ruleset.
        // You may add further detailed validation based on the rules for pchar, segment, etc., from the ruleset
        if (!string.IsNullOrEmpty(path) && !Regex.IsMatch(path, @"^(/[\w:@!$&'()*+,.=]*)*$"))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(query) && !Regex.IsMatch(query, @"^([\w:@!$&'()*+,.=]*[/?]*)*$"))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(fragment) && !Regex.IsMatch(fragment, @"^([\w:@!$&'()*+,-.=]*[/?]*)*$"))
        {
            return false;
        }

        // If everything is valid according to the ruleset, we populate the result object.
        result = new ParsedDidUrl(methodName, network, subNetwork, methodSpecificId, path, query, fragment);
        return true;
    }
}