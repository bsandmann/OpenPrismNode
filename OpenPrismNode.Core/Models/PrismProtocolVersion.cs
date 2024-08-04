namespace OpenPrismNode.Core.Models;

public sealed class PrismProtocolVersion
{
    public PrismProtocolVersion(int majorVersion, int minorVersion)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
    } 
    
    /// <summary>
    /// If minor value changes, the node can opt to not update. All events _published_ by this node would be also
    /// understood by other nodes with the same major version. However, there may be new events that this node won't _read_
    /// </summary>
    public int MinorVersion { get; }
    
    /// <summary>
    /// If major value changes, the node MUST stop issuing and reading operations, and upgrade before
    /// `effective_since` because the new protocol version.
    /// </summary>
    public int MajorVersion { get; } 
}