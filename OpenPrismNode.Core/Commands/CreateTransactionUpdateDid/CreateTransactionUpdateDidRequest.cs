namespace OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Commands;
using OpenPrismNode.Core.Models;

/// <summary>
/// Request
/// </summary>
public class CreateTransactionUpdateDidRequest : TransactionBaseRequest, IRequest<Result<TransactionModel>>
{
    /// <summary>
    /// Request Constructor
    /// </summary>
    /// <param name="transactionHash"></param>
    /// <param name="blockHeight"></param>
    /// <param name="fees"></param>
    /// <param name="size"></param>
    /// <param name="index"></param>
    /// <param name="operationHash"></param>
    /// <param name="previousOperationHash"></param>
    /// <param name="did"></param>
    /// <param name="signingKeyId"></param>
    /// <param name="updateDidActions"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="blockHash"></param>
    /// <param name="utxos"></param>
    public CreateTransactionUpdateDidRequest(Hash transactionHash, Hash blockHash, int blockHeight,int fees, int size, int index, Hash operationHash, Hash previousOperationHash, string did, string signingKeyId, List<UpdateDidActionResult> updateDidActions, int operationSequenceNumber, List<UtxoWrapper> utxos)
        : base(transactionHash, blockHash, blockHeight, fees, size, index, utxos)
    {
        OperationHash = operationHash;
        PreviousOperationHash = previousOperationHash;
        SigningKeyId = signingKeyId;
        UpdateDidActions = updateDidActions;
        OperationSequenceNumber = operationSequenceNumber;
        Did = did;
    }

    /// <summary>
    /// List of the update-actions which should be applied. These could be addKey-action and removeKey-actions both mixed
    /// </summary>
    public List<UpdateDidActionResult> UpdateDidActions { get; }
    
    /// <summary>
    /// In case multiple PRISM-Operations are in one transaction, this number defines the position in the transaction
    /// eg. a DidCreateOperation may have 0 and the following IssueCredentialBatchOperation has 1.
    /// </summary>
    public int OperationSequenceNumber { get; }

    /// <summary>
    /// The Did which gets the updates keys
    /// </summary>
    public string Did { get; }

    ///  /// <summary>
    /// Hash of this PRISM-operation
    /// </summary>
    public Hash OperationHash { get; }
    
    ///  /// <summary>
    ///  Hash of the previous PRISM-operation
    /// </summary>
    public Hash PreviousOperationHash { get; }

    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
    public string SigningKeyId { get; }
}