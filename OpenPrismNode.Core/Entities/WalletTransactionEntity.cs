namespace OpenPrismNode.Core.Entities;

public class WalletTransactionEntity
{
    public int WalletTransactionEntityId { get; set; }
   
    /// <summary>
    /// The transactionId for the transaction written on chain
    /// </summary>
    public string TransactionId { get; set; }
    
    
    /// <summary>
    /// Timestamp when the transaction was created internally
    /// </summary>
    public DateTime CreatedUtc { get; set; }
    
    /// <summary>
    /// Timestamp when the transaction was last synced
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }
    
    /// <summary>
    /// The depth of the Transaction when last synced
    /// </summary>
    public long Depth { get; set; }
    
    /// <summary>
    /// Fee for the transaction
    /// </summary>
    public long Fee { get; set; }
    
    /// <summary>
    /// Wallet the transaction belongs to
    /// </summary>
    public int WalletEntityId { get; set; }
    
    /// <summary>
    /// Wallet the transaction belongs to
    /// </summary>
    public WalletEntity Wallet { get; set; }
    
    /// <summary>
    /// The OperationStatus this transaction belongs to
    /// </summary>
    public int? OperationStatusEntityId { get; set; }
    
    /// <summary>
    /// The OperationStatus this transaction belongs to
    /// </summary>
    public OperationStatusEntity? OperationStatusEntity { get; set; }
}
