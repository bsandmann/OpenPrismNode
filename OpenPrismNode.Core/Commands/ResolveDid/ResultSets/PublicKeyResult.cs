namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;

using OpenPrismNode.Core.Models;

/// <summary>
/// Result of of Select operation againt the db
/// </summary>
public class PublicKeyResult
{
    /// <summary>
    /// PublicKey as byte[65]
    /// </summary>
    public byte[] PublicKey { get; set; } = null!;
    
    /// <summary>
    /// KeyId e.g. master0
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// KeyUsage
    /// </summary>
    public PrismKeyUsage PrismKeyUsage { get; set; }

    /// <summary>
    /// Curve, eg. "secp256k1"
    /// </summary>
    public string Curve { get; set; }

    /// <summary>
    /// Optional property for the updateOperation
    /// </summary>
    public int? UpdateOperationOrder { get; set; }
}