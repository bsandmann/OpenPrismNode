namespace OpenPrismNode.Core.Models;

public class TransactionModel
{
    public TransactionModel(Hash transactionHash, Hash blockHash, int fees, int size, int index)//, List<PrismCreateDidModel> createDidOperations, List<PrismUpdateDidModel> updateDidOperations, List<PrismDeactivateDidModel> deactivateDidOperations, List<PrismIssueCredentialBatchModel> issueCredentialBatchOperations,
//        List<PrismRevokeCredentialsModel> revokeCredentialsOperations, List<PrismProtocolVersionUpdateModel> protocolVersionUpdateOperations, List<PrismUtxoModel> incomingUtxos, List<PrismUtxoModel> outgoingUtxos)
    {
        TransactionHash = transactionHash;
        BlockHash = blockHash;
        Fees = fees;
        Size = size;
        Index = index;
        // Label = label;
        // CreateDidOperations = createDidOperations;
        // UpdateDidOperations = updateDidOperations;
        // IssueCredentialBatchOperations = issueCredentialBatchOperations;
        // DeactivateDidOperations = deactivateDidOperations;
        // RevokeCredentialsOperations = revokeCredentialsOperations;
        // ProtocolVersionUpdateOperations = protocolVersionUpdateOperations;
        // IncomingUtxos = incomingUtxos;
        // OutgoingUtxos = outgoingUtxos;
    }

    /// <summary>
    /// Identifier of the Transaction
    /// </summary>
    public Hash TransactionHash { get; }

    /// <summary>
    /// Reference to the block this transactions lives in
    /// </summary>
    public Hash BlockHash { get; }

    /// <summary>
    /// Fees creating this transaction in lovelace
    /// </summary>
    public int Fees { get; }

    /// <summary>
    /// Size of the transaction in bytes
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// The transaction index inside the underlying block.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The metadata label. might be identical for all PRISM operations
    /// </summary>
    // public int Label { get; }

    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismCreateDidModel> CreateDidOperations { get; }

    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismUpdateDidModel> UpdateDidOperations { get; }
    
    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismDeactivateDidModel> DeactivateDidOperations { get; }

    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismIssueCredentialBatchModel> IssueCredentialBatchOperations { get; }

    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismRevokeCredentialsModel> RevokeCredentialsOperations { get; }

    /// <summary>
    /// A transaction contains one of the following 5 operations:
    /// </summary>
    // public List<PrismProtocolVersionUpdateModel> ProtocolVersionUpdateOperations { get; }
    
    /// <summary>
    /// List of incoming Utxos with value and wallet addresses
    /// </summary>
    // public List<PrismUtxoModel> IncomingUtxos { get; }
    
    /// <summary>
    /// List of outgoing Utxos with value and wallet addresses
    /// </summary>
    // public List<PrismUtxoModel> OutgoingUtxos { get; }
}