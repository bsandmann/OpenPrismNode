namespace OpenPrismNode.Core.Models;

public class WalletAddress
{
    /// <summary>
    /// WalletAddress as string
    /// Shortest and longest example found in the db
    ///  addr_test1vqytjd593rsk6w3azlfv59y5fgdqpev4jrc4ca4g0mt9zwqkvw42s
    ///  37btjrVyb4KEEmc884pz5HUcpTLMb58s4NQh1Fjvy6q8dw2ATyFcUfy7PTEA2qGQ968nGqyvBdhg4FBCjxvjEp8oq4wpnzewip6DFPyzJnH4Ke9dsA
    /// </summary>
    public string WalletAddressString { get; set; }

    /// <summary>
    /// StakeAddress as string
    /// Examples. Always identical in length
    /// stake_test1uq7g7kqeucnqfweqzgxk3dw34e8zg4swnc7nagysug2mm4cm77jrx
    /// stake_test1urej3yc8n9du5kyzkympsnqlh30uqal2ry2k3glukvc8tnshh4nqz
    /// </summary>
    public string? StakeAddressString { get; set; }
}