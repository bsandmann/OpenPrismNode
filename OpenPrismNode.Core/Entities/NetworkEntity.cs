namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using OpenPrismNode.Core.Models;

public class NetworkEntity
{
    /// <summary>
    /// Configured network (InMemory, prerpod, mainnet)
    /// </summary>
    public required LedgerType NetworkType { get; set; }
    
    /// <summary>
    /// Last synced Block
    /// </summary>
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? LastSynced { get; set; }

    /// <summary>
    /// Referencing all epochs
    /// </summary>
    public List<EpochEntity> Epochs { get; set; } = new List<EpochEntity>();
}