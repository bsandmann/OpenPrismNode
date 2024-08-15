namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class WalletAddressEntity
{
    // PK - exmaples
    // addr_test1vqytjd593rsk6w3azlfv59y5fgdqpev4jrc4ca4g0mt9zwqkvw42s
    // 37btjrVyb4KEEmc884pz5HUcpTLMb58s4NQh1Fjvy6q8dw2ATyFcUfy7PTEA2qGQ968nGqyvBdhg4FBCjxvjEp8oq4wpnzewip6DFPyzJnH4Ke9dsA
    [MaxLength(114)]
    public required string WalletAddress { get; set; }

    /// <summary>
    /// Reference Utxos
    /// </summary>
    public List<UtxoEntity> Utxos { get; set; } = new List<UtxoEntity>();
}