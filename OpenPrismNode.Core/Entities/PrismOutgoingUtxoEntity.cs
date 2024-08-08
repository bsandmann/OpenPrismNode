namespace OpenPrismNode.Core.Entities;

public class PrismOutgoingUtxoEntity
{
    /// <summary>
    /// IK
    /// </summary>
    public long PrismOutgoingUtxoId { get; set; }

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public byte[] TransactionHash { get; set; } = null!;

    /// <summary>
    /// Index of the operations related to other incoming or outgoing utxos for the same transaction
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// Value in lovelace moved in the utxo
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Wallet-Address used in this Utxo
    /// </summary>
    public PrismWalletAddressEntity PrismWalletAddress { get; set; }
    //
    /// <summary>
    /// Wallet-Address used in this Utxo
    /// </summary>
    public string WalletAddressString { get; set; } = String.Empty;

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public PrismTransactionEntity PrismTransactionEntity { get; set; } = null!;
}