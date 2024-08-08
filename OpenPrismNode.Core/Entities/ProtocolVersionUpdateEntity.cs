namespace OpenPrismNode.Core.Entities;

/// <summary>
/// PrismProtocolVersionUpdateEntities 
/// </summary>
public class ProtocolVersionUpdateEntity : BaseOperationEntity
{
    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
    public string SigningKeyId { get; set; }

    /// <summary>
    /// Cardano block number that tells since which block the update is enforced
    /// </summary>
    public long EffectiveSinceBlock { get; set; }

    /// <summary>
    /// new minor version to be announced, if this value changes, the node can opt to not update. All events _published_ by this node would be also
    /// understood by other nodes with the same major version. However, there may be new events that this node won't _read_
    /// </summary>
    public int MinorVersion { get; set; }

    /// <summary>
    /// new major version to be announced, if this value is changed, the node MUST stop issuing and reading operations,
    /// and upgrade before `effective_since` because the new protocol version
    /// modifies existing events. This implies that some events _published_ by this node would stop being valid for nodes in newer version
    /// </summary>
    public int MajorVersion { get; set; }

    /// <summary>
    /// (optional) name of the version
    /// </summary>
    public string VersionName { get;set; }
}