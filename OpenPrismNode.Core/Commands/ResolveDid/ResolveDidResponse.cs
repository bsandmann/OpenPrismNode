namespace OpenPrismNode.Core.Commands.ResolveDid;

using Models;

public class ResolveDidResponse
{
    public ResolveDidResponse(InternalDidDocument internalDidDocument, Hash lastOperationHash)
    {
        InternalDidDocument = internalDidDocument;
        LastOperationHash = lastOperationHash;
    }

    /// <summary>
    /// The result of the resolving operationg
    /// </summary>
    public InternalDidDocument InternalDidDocument { get; }

    /// <summary>
    /// The last operationHash of the last valid updateOperation or
    /// createDid Operation which was found in the database
    /// </summary>
    public Hash LastOperationHash { get; }
}