namespace OpenPrismNode.Core.Commands;

using Models;

/// <summary>
/// Request
/// </summary>
public class TransactionBaseRequest
{
    /// <summary>
    /// Base TransactionRequest
    /// </summary>
    /// <param name="transactionHash"></param>
    /// <param name="blockHash"></param>
    /// <param name="blockHeight"></param>
    /// <param name="fees"></param>
    /// <param name="size"></param>
    /// <param name="index"></param>
    /// <param name="utxos"></param>
    public TransactionBaseRequest(Hash transactionHash, Hash blockHash, int blockHeight, int fees, int size, int index, List<UtxoWrapper> utxos)
    {
        TransactionHash = transactionHash;
        BlockHash = blockHash;
        BlockHeight = blockHeight;
        Fees = fees;
        Size = size;
        Index = index;
        Utxos = utxos;
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
    /// Reference to the block this transactions lives in
    /// </summary>
    public int BlockHeight { get; }

    /// <summary>
    /// Fees creating this transaction in lovelace
    /// </summary>
    public int Fees { get; }

    /// <summary>
    /// Size of the transaction in bytes
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// OperationSequenceNumber
    /// </summary>
    public int Index { get; }


    /// <summary>
    /// List of incoming and outgoing Utxos with value and wallet and staking addresses
    /// </summary>
    public List<UtxoWrapper> Utxos { get; }
}