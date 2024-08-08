namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using OpenPrismNode.Core.Models;

public class EpochEntity
{
    /// <summary>
    /// Just the Epoch-Number
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public uint Epoch { get; set; }

    /// <summary>
    /// Reference to the Block-Entities
    /// </summary>
    public List<BlockEntity> PrismBlockEntities { get; set; } = new List<BlockEntity>();

    /// <summary>
    /// Reference to the network
    /// </summary>
    public NetworkEntity NetworkEntity { get; set; }  // don't add a default value here!
    
    /// <summary>
    /// Network Type
    /// </summary>
    public LedgerType NetworkType { get; set; }
}