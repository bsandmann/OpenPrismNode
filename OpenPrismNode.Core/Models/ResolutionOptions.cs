namespace OpenPrismNode.Core.Models;

public class ResolutionOptions
{
    // The media type of the caller's preferred representation of the DID document
    public string Accept { get; set; }

    // Option to disable caching and retrieve a fresh DID document
    public bool NoCache { get; set; } = false;

    // Option to request a specific version of the DID document
    public string VersionId { get; set; }

    // Option to request the DID document as it existed at a specific time
    public DateTimeOffset? VersionTime { get; set; }

    // // Option to control whether redirects should be followed
    // public bool? FollowRedirect { get; set; }
    //
    // // Additional method-specific options
    // public Dictionary<string, string> MethodSpecificOptions { get; set; } = new Dictionary<string, string>();

    public bool IsValid()
    {
        return !(VersionId != null && VersionTime.HasValue);
    }
}