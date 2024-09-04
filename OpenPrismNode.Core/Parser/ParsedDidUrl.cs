namespace OpenPrismNode.Core.Parser;

using System.Collections.Specialized;
using System.Web;

public record ParsedDidUrl
{
    /// <summary>
    /// eg. prism, web, key, etc.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Identifer
    /// </summary>
    public string MethodSpecificId { get; }

    /// <summary>
    /// Network e.g. indy:sovrin or testnet, mainnet, cardano ...
    /// Optional
    /// </summary>
    public string? Network { get; init; } = null;

    /// <summary>
    /// SubNetwork e.g. staging, builder, etc. Not every method has this. It may then look like this:
    /// did:indy:sovrin:builder:123
    /// </summary>
    public string? SubNetwork { get; init; } = null;

    /// <summary>
    /// Optional path e.g "/foo/bar"
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Full query string e.g. "a=b&c=d". May consist of multiple query keys
    /// While querykeys could potentially be every string, the spec registry defines a set of common ones
    /// https://www.w3.org/TR/did-spec-registries/#parameters
    /// </summary>
    public string? Query { get; }

    /// <summary>
    /// Frgament to reference a specific verification Method
    /// </summary>
    public string? Fragment { get; }

    /// <summary>
    /// Parsed set of each query parameter
    /// </summary>
    private readonly NameValueCollection _queryParams;

    public ParsedDidUrl(string methodName, string? network, string? subnetwork, string methodSpecificId, string? path, string? query, string? fragment)
    {
        MethodName = methodName.ToLowerInvariant();
        Network = network?.ToLowerInvariant();
        SubNetwork = subnetwork?.ToLowerInvariant();
        MethodSpecificId = methodSpecificId; // Can potentially be case-sensitive!
        Path = path; // Can potentially be case-sensitive!
        Query = query; // Can potentially be case-sensitive!
        Fragment = fragment; // Can potentially be case-sensitive!

        if (!string.IsNullOrEmpty(query))
        {
            _queryParams = HttpUtility.ParseQueryString(query);
        }
        else
        {
            _queryParams = new NameValueCollection();
        }
    }

    public string? GetQueryParameter(string paramName)
    {
        return _queryParams[paramName];
    }

    public string?[] GetQueryKeys()
    {
        return _queryParams.AllKeys;
    }

    public override string ToString()
    {
        return GetDidString();
    }

    public bool IsBareDid()
    {
        return string.IsNullOrEmpty(Path) && string.IsNullOrEmpty(Query) && string.IsNullOrEmpty(Fragment);
    }

    /// <summary>
    /// Returns the Did string without the path, query and fragment
    /// </summary>
    /// <returns></returns>
    public string GetDidString()
    {
        if (string.IsNullOrEmpty(Network))
        {
            return $"did:{MethodName}:{MethodSpecificId}";
        }
        else if (string.IsNullOrEmpty(SubNetwork))
        {
            return $"did:{MethodName}:{Network}:{MethodSpecificId}";
        }
        else
        {
            return $"did:{MethodName}:{Network}:{SubNetwork}:{MethodSpecificId}";
        }
    }

    /// <summary>
    /// Returns the Did string with the path, query and fragment
    /// </summary>
    /// <returns></returns>
    public string GetDidUrlString()
    {
        string url;
        if (string.IsNullOrEmpty(Network))
        {
            url = $"did:{MethodName}:{MethodSpecificId}{Path}";
        }
        else if (string.IsNullOrEmpty(SubNetwork))
        {
            url = $"did:{MethodName}:{Network}:{MethodSpecificId}{Path}";
        }
        else
        {
            url = $"did:{MethodName}:{Network}:{SubNetwork}:{MethodSpecificId}{Path}";
        }

        if (!string.IsNullOrEmpty(Query))
        {
            url += "?" + Query;
        }

        if (!string.IsNullOrEmpty(Fragment))
        {
            url += "#" + Fragment;
        }

        return url;
    }

    public virtual bool Equals(ParsedDidUrl? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;

        var other = (ParsedDidUrl)obj;

        // Compare public properties and also _queryParams
        return MethodName == other.MethodName &&
               Network == other.Network &&
               SubNetwork == other.SubNetwork &&
               MethodSpecificId == other.MethodSpecificId &&
               Path == other.Path &&
               Query == other.Query &&
               Fragment == other.Fragment &&
               _queryParams.AllKeys.SequenceEqual(other._queryParams.AllKeys) &&
               _queryParams.AllKeys.All(key => _queryParams[key] == other._queryParams[key]);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(MethodName);
        hashCode.Add(Network);
        hashCode.Add(SubNetwork);
        hashCode.Add(MethodSpecificId);
        hashCode.Add(Path);
        hashCode.Add(Query);
        hashCode.Add(Fragment);
        foreach (var key in _queryParams.AllKeys)
        {
            hashCode.Add(key);
            hashCode.Add(_queryParams[key]);
        }

        return hashCode.ToHashCode();
    }
}