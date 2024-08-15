namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
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
    public int PrismPublicKeyEntityId { get; set; }

    /// <summary>
    /// PublicKey as byte[65]
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] PublicKey { get; set; }
    
    /// <summary>
    /// KeyId e.g. master0
    /// </summary>
    [MaxLength(50)]
    public string KeyId { get; set; }

    /// <summary>
    /// KeyUsage
    /// </summary>
    public PrismKeyUsage PrismKeyUsage { get; set; }
    
    /// <summary>
    /// Curve, eg. "secp256k1"
    /// </summary>
    [MaxLength(12)]
    public string Curve { get; set; }

    /// <summary>
    /// References to the linked operation which created this key
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] CreateDidEntityOperationHash { get; set; }
    
    //
    // /// <summary>
    // /// References to the linked operation which added this key
    // /// </summary>
    // // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // public UpdateDidEntity? PrismUpdateDidEntity { get; set; }
}