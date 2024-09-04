namespace OpenPrismNode.Core.Models;

public class OperationResultWrapper
{
    /// <summary>
    /// Constructor for CreateDid-operation
    /// </summary>
    /// <param name="operationResultType"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="internalDidDocument"></param>
    /// <param name="signingKeyId"></param>
    /// <exception cref="ArgumentException"></exception>
    public OperationResultWrapper(OperationResultType operationResultType, int operationSequenceNumber, InternalDidDocument internalDidDocument, string signingKeyId)
    {
        if (operationResultType == OperationResultType.CreateDid)
        {
            OperationSequenceNumber = operationSequenceNumber;
            OperationResultType = operationResultType;
            InternalDidDocument = internalDidDocument;
            SigningKeyId = signingKeyId;
        }
        else
        {
            throw new ArgumentException();
        }
    }
    //
    /// <summary>
    /// Constructor for UpdateDid-operation
    /// </summary>
    /// <param name="operationResultType"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="didIdentifier"></param>
    /// <param name="previousOperationHash"></param>
    /// <param name="updateDidActionResults"></param>
    /// <param name="operationBytes"></param>
    /// <param name="signature"></param>
    /// <param name="signingKeyId"></param>
    /// <exception cref="ArgumentException"></exception>
    public OperationResultWrapper(OperationResultType operationResultType, int operationSequenceNumber, string didIdentifier, Hash previousOperationHash, List<UpdateDidActionResult> updateDidActionResults, byte[] operationBytes, byte[] signature, string signingKeyId)
    {
        if (operationResultType == OperationResultType.UpdateDid)
        {
            OperationSequenceNumber = operationSequenceNumber;
            OperationResultType = operationResultType;
            DidIdentifer = didIdentifier;
            PreviousOperationHash = previousOperationHash;
            UpdateDidActionResults = updateDidActionResults;
            OperationBytes = operationBytes;
            Signature = signature;
            SigningKeyId = signingKeyId;
        }
        else
        {
            throw new ArgumentException();
        }
    }


    /// <summary>
    /// Constructor for ProtocolVersionUpdate-Operation  
    /// </summary>
    /// <param name="operationResultType"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="protocolVersionUpdate"></param>
    /// <param name="proposerDidIdentifier"></param>
    /// <param name="operationBytes"></param>
    /// <param name="signature"></param>
    /// <param name="signingKeyId"></param>
    /// <exception cref="ArgumentException"></exception>
    public OperationResultWrapper(OperationResultType operationResultType, int operationSequenceNumber, ProtocolVersionUpdate protocolVersionUpdate, string proposerDidIdentifier, byte[] operationBytes, byte[] signature, string signingKeyId)
    {
        if (operationResultType == OperationResultType.ProtocolVersionUpdate)
        {
            OperationSequenceNumber = operationSequenceNumber;
            OperationResultType = operationResultType;
            ProtocolVersionUpdate = protocolVersionUpdate;
            DidIdentifer = proposerDidIdentifier;
            OperationBytes = operationBytes;
            Signature = signature;
            SigningKeyId = signingKeyId;
        }
        else
        {
            throw new ArgumentException();
        }
    }
    
    /// <summary>
    /// Constructor for DeactivateDid-Operation  
    /// </summary>
    /// <param name="operationResultType"></param>
    /// <param name="operationSequenceNumber"></param>
    /// <param name="operationBytes"></param>
    /// <param name="signature"></param>
    /// <param name="signingKeyId"></param>
    /// <param name="didIdentifier"></param>
    /// <param name="previousOperationHash"></param>
    /// <exception cref="ArgumentException"></exception>
    public OperationResultWrapper(OperationResultType operationResultType, int operationSequenceNumber, string didIdentifier, Hash previousOperationHash,byte[] operationBytes, byte[] signature, string signingKeyId)
    {
        if (operationResultType == OperationResultType.DeactivateDid)
        {
            OperationSequenceNumber = operationSequenceNumber;
            OperationResultType = operationResultType;
            DidIdentifer = didIdentifier;
            PreviousOperationHash = previousOperationHash;
            OperationBytes = operationBytes;
            Signature = signature;
            SigningKeyId = signingKeyId;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    public OperationResultType OperationResultType { get; }
    public int OperationSequenceNumber { get; }
    private InternalDidDocument? InternalDidDocument { get; }
    private string? DidIdentifer { get; }
    
    private Hash? PreviousOperationHash { get; }
    private List<UpdateDidActionResult>? UpdateDidActionResults { get; }
    
    /// <summary>
    /// The Hash of the Operation to later be verified
    /// Note: The createdDid-Operation automatically gets verified while parsing, because the public-Key is already available
    /// </summary>
    private byte[]? OperationBytes { get; }
    //
    /// <summary>
    /// The signature the Operation was signed with to verify is the operation is valid and not tempered with
    /// </summary>
    private byte[]? Signature { get; }
    //
    /// <summary>
    /// The signingKeyId to find the corresponding publicKey to the signature
    /// </summary>
    private string SigningKeyId { get; }
    
    
    private ProtocolVersionUpdate? ProtocolVersionUpdate { get; }
    
    public (InternalDidDocument didDocument, string signingKeyId) AsCreateDid()
    {
        return (InternalDidDocument!, SigningKeyId);
        
    }
    public (string didIdentifier, Hash previousOperationHash, List<UpdateDidActionResult> updateDidActionResults, byte[] operationBytes, byte[] signature, string signingKeyId) AsUpdateDid()
    {
        return (DidIdentifer!, PreviousOperationHash!, UpdateDidActionResults!, OperationBytes!, Signature!, SigningKeyId);
    }
  
    public (string deactivatedDid, Hash previousOperationHash, byte[] operationBytes, byte[] signature, string signingKeyId) AsDeactivateDid()
    {
        return (DidIdentifer!, PreviousOperationHash!, OperationBytes!, Signature!, SigningKeyId);
    }
}