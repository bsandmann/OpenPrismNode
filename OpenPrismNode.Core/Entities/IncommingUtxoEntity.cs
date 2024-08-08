namespace OpenPrismNode.Core.Entities;

public class IncomingUtxoEntity
{
    /// <summary>
    /// IK
    /// </summary>
    public long PrismIncomingUtxoId { get; set; }
    
    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public byte[] TransactionHash { get; set; } = null!;
    
    /// <summary>
    /// Index of the operations related to other incoming or outgoing utxos for the same transaction
    /// </summary>
    public uint Index {get; set; }
    
    /// <summary>
    /// Value in lovelace moved in the utxo
    /// </summary>
    public long Value { get; set; }
   
    /// <summary>
    /// Wallet-Address used in this Utxo
    /// </summary>
    public WalletAddressEntity WalletAddress { get; set; }
   
    /// <summary>
    /// Wallet-Address used in this Utxo
    /// </summary>
    public string WalletAddressString { get; set; } = String.Empty;
    
    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public TransactionEntity TransactionEntity { get; set; } = null!;

}