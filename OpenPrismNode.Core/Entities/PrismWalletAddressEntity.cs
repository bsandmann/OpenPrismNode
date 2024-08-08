namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class PrismWalletAddressEntity
{
    // PK
    //  addr_test1vqytjd593rsk6w3azlfv59y5fgdqpev4jrc4ca4g0mt9zwqkvw42s
    //  37btjrVyb4KEEmc884pz5HUcpTLMb58s4NQh1Fjvy6q8dw2ATyFcUfy7PTEA2qGQ968nGqyvBdhg4FBCjxvjEp8oq4wpnzewip6DFPyzJnH4Ke9dsA
    [MinLength(63)]
    [MaxLength(114)]
    public string? WalletAddressString { get; set; }

    // Wahrschienlich muss ich die Stakeadress trennen...
    // stake_test1uq7g7kqeucnqfweqzgxk3dw34e8zg4swnc7nagysug2mm4cm77jrx
    // stake_test1urej3yc8n9du5kyzkympsnqlh30uqal2ry2k3glukvc8tnshh4nqz
    [StringLength(64)]
    public string? StakeAddress { get; set; }

    /// <summary>
    /// Reference Utxos
    /// </summary>
    public List<PrismIncomingUtxoEntity> IncomingUtxos { get; set; } = new List<PrismIncomingUtxoEntity>();

    /// <summary>
    /// Reference Utxos
    /// </summary>
    public List<PrismOutgoingUtxoEntity> OutgoingUtxos { get; set; } = new List<PrismOutgoingUtxoEntity>();
}