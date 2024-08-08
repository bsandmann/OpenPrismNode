namespace OpenPrismNode.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using OpenPrismNode.Core.Models;

/// <summary>
/// PrismPublicKeyEntity 
/// </summary>
public class PrismPublicKeyEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public uint PrismPublicKeyEntityId { get; set; }

    /// <summary>
    /// PublicKey as byte[65]
    /// </summary>
    [Column(TypeName = "binary(65)")]
    public byte[] PublicKey { get; set; }

    /// <summary>
    /// KeyUsage
    /// </summary>
    public PrismKeyUsage PrismKeyUsage { get; set; }
    
    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    public int? UpdateOperationOrder { get; set; }
    
    /// <summary>
    /// Curve, eg. "secp256k1"
    /// </summary>
    public string Curve { get; set; }

    /// <summary>
    /// KeyId e.g. master0
    /// </summary>
    public string KeyId { get; set; }

    /// <summary>
    /// References to the linked operation which created this key
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public CreateDidEntity? PrismCreateDidEntity { get; set; }
    
    /// <summary>
    /// References to the linked operation which added this key
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public UpdateDidEntity? PrismUpdateDidEntity { get; set; }
}