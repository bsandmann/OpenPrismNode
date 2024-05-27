namespace OpenPrismNode.Core.Models;

public sealed class PrismProtocolVersionUpdate
{
    public PrismProtocolVersionUpdate(PrismProtocolVersion? prismProtocolVersion, string versionName, int effectiveSinceBlock, string proposerDidIdentifier)
    {
        PrismProtocolVersion = prismProtocolVersion;
        VersionName = versionName;
        EffectiveSinceBlock = effectiveSinceBlock;
        ProposerDidIdentifier = proposerDidIdentifier;
    }

    /// <summary>
    /// Cardano block number that tells since which block the update is enforced
    /// </summary>
    public int EffectiveSinceBlock { get; }

    public PrismProtocolVersion? PrismProtocolVersion { get; }

    /// <summary>
    /// (optional) name of the version
    /// </summary>
    public string VersionName { get; }
   
    /// <summary>
    /// Proposer
    /// </summary>
    public string ProposerDidIdentifier { get; }
}