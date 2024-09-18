namespace OpenPrismNode.Core.Entities;

public class WalletEntity
{
    public int WalletEntityId { get; set; }

    /// <summary>
    /// The mnemonic phrase for the wallet, seperated by spaces.
    /// </summary>
    public string Mnemonic { get; set; }

    /// <summary>
    /// The passphrase for the wallet. Needed to sign transactions.
    /// </summary>
    public string Passphrase { get; set; }

    /// <summary>
    /// A user friendly name for the wallet.
    /// </summary>
    public string WalletName { get; set; }

    /// <summary>
    /// The internal wallet id - used for all wallet-operations in the API
    /// </summary>
    public string WalletId { get; set; }

    /// <summary>
    /// Flag if the wallet was synced initially
    /// </summary>
    public bool IsSyncedInitially { get; set; }
    
    /// <summary>
    /// Progress for the initiall sync
    /// </summary>
    public int? SyncProgress { get; set; }
    
    /// <summary>
    /// Flag if the wallet is currently in sync
    /// </summary>
    public bool? IsInSync { get; set; }

    /// <summary>
    /// The last known balance of the wallet, after the last sync
    /// </summary>
    public long? LastKnownBalance { get; set; }
    
    /// <summary>
    /// Timestamp when the wallet was created
    /// </summary>
    public DateTime CreatedUtc { get; set; }
    
    /// <summary>
    /// Timestamp when the wallet was last synced
    /// </summary>
    public DateTime? LastSynced { get; set; }

    /// <summary>
    /// The primary address of the wallet for funding and sending transactions
    /// </summary>
    public string? FundingAddress { get; set; }
    
    //FK
    public ICollection<WalletTransactionEntity> WalletTransactions { get; set; }

}