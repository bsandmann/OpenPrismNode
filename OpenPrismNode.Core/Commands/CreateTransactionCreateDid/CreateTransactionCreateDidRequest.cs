namespace OpenPrismNode.Core.Commands.CreateTransactionCreateDid;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Commands;
using OpenPrismNode.Core.Models;

/// <summary>
/// Request
/// </summary>
public class CreateTransactionCreateDidRequest : TransactionBaseRequest, IRequest<Result>
{
    /// <summary>
    /// Request Constructor
    /// </summary>
    /// <param name="transactionHash"></param>
    /// <param name="blockHash"></param>
    /// <param name="blockHeight"></param>
    /// <param name="fees"></param>
    /// <param name="size"></param>
    /// <param name="index"></param>
    /// <param name="operationHash"></param>
    /// <param name="did"></param>
    /// <param name="signingKeyId"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="utxos"></param>
    /// <param name="prismPublicKeys"></param>
    /// <param name="prismServices"></param>
    /// <param name="patchedContexts"></param>
    public CreateTransactionCreateDidRequest(Hash transactionHash, Hash blockHash, int blockHeight, int fees, int size, int index, Hash operationHash, string did, string signingKeyId,
        int operationSequenceNumber, List<UtxoWrapper> utxos, List<PrismPublicKey> prismPublicKeys, List<PrismService> prismServices, List<string>? patchedContexts = null)
        : base(transactionHash, blockHash, blockHeight, fees, size, index, utxos)
    {
        OperationHash = operationHash;
        Did = did;
        SigningKeyId = signingKeyId;
        OperationSequenceNumber = operationSequenceNumber;
        PrismPublicKeys = prismPublicKeys;
        PrismServices = prismServices;
        PatchedContexts = patchedContexts;
    }

    /// <summary>
    /// List of the public keys, their signingKeyId and their usage
    /// </summary>
    public List<PrismPublicKey> PrismPublicKeys { get; }

    /// <summary>
    /// List of the Services
    /// </summary>
    public List<PrismService> PrismServices { get; }
    
    /// <summary>
    /// Optional list of patched contexts
    /// </summary>
    public List<string>? PatchedContexts { get;  }

    ///  /// <summary>
    /// Hash of the Prism-operation
    /// </summary>
    public Hash OperationHash { get; }

    /// <summary>
    /// In case multiple PRISM-Operations are in one transaction, this number defines the position in the transaction
    /// eg. a DidCreateOperation may have 0 and the following IssueCredentialBatchOperation has 1.
    /// </summary>
    public int OperationSequenceNumber { get;  }

    /// <summary>
    /// The Did created
    /// </summary>
    public string Did { get; }

    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
    public string SigningKeyId { get; }
}