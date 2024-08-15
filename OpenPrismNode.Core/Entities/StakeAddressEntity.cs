namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class StakeAddressEntity
{
    // PK - exmaples 
    // stake_test1uq7g7kqeucnqfweqzgxk3dw34e8zg4swnc7nagysug2mm4cm77jrx
    // stake_test1urej3yc8n9du5kyzkympsnqlh30uqal2ry2k3glukvc8tnshh4nqz
    [StringLength(64)]
    public required string StakeAddress { get; set; }

    /// <summary>
    /// Reference Utxos
    /// </summary>
    public List<UtxoEntity> Utxos { get; set; } = new List<UtxoEntity>();
}