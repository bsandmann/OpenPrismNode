namespace OpenPrismNode.Core.Commands.CreateTransactionDeactivateDid;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Commands;
using OpenPrismNode.Core.Models;

public class CreateTransactionDeactivateDidRequest : TransactionBaseRequest, IRequest<Result>
{
    public CreateTransactionDeactivateDidRequest(Hash transactionHash, Hash blockHash, int blockHeight, int fees, int size, int index, Hash operationHash, Hash previousOperationHash, string did, string signingKeyId, int operationSequenceNumber,
        List<UtxoWrapper> utxos)
        : base(transactionHash, blockHash, blockHeight, fees, size, index, utxos)
    {
        OperationHash = operationHash;
        PreviousOperationHash = previousOperationHash;
        SigningKeyId = signingKeyId;
        OperationSequenceNumber = operationSequenceNumber;
        Did = did;
    }

    /// <summary>
    /// The Did which gets deactivated
    /// </summary>
    public string Did { get; }

    /// <summary>
    /// In case multiple PRISM-Operations are in one transaction, this number defines the position in the transaction
    /// eg. a DidCreateOperation may have 0 and the following IssueCredentialBatchOperation has 1.
    /// </summary>
    public int OperationSequenceNumber { get; }

    /// <summary>
    /// Hash of this PRISM-operation
    /// </summary>
    public Hash OperationHash { get; }

    ///  /// <summary>
    ///  Hash of the previous PRISM-operation
    /// </summary>
    public Hash PreviousOperationHash { get; }

    /// <summary>
    /// The signing Key used in the DeactivateDid Operation
    /// </summary>
    public string SigningKeyId { get; }
}