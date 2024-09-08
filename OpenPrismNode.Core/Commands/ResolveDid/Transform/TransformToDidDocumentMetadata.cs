namespace OpenPrismNode.Core.Commands.ResolveDid.Transform;

using Models;
using Models.DidDocument;

public static class TransformToDidDocumentMetadata
{
    public static DidDocumentMetadata Transform(InternalDidDocument internalDidDocument, LedgerType ledger, bool includeNetworkIdentifier)
    {
        var networkIdentifier = string.Empty;
        if (includeNetworkIdentifier)
        {
            if (ledger == LedgerType.CardanoMainnet)
            {
                networkIdentifier = "mainnet:";
            }
            else if (ledger == LedgerType.CardanoPreprod)
            {
                networkIdentifier = "preprod:";
            }
        }

        var did = $"did:prism:{networkIdentifier}{internalDidDocument.DidIdentifier}";


        var didDocumentMetadata = new DidDocumentMetadata()
        {
            CanonicalId = did,
            Created = internalDidDocument.Created,
            Updated = internalDidDocument.Updated,
            VersionId = internalDidDocument.VersionId,
            Deactivated = internalDidDocument.Deactivated == false ? null : internalDidDocument.Deactivated,
            // TODO NextUpdate 
            CardanoTransactionPosition = internalDidDocument.CardanoTransactionPosition,
            OperationPosition = internalDidDocument.OperationPosition,
            OriginTxId = internalDidDocument.OriginTxId,
            UpdateTxId = internalDidDocument.UpdateTxId,
            DeactivateTxId = internalDidDocument.DeactivateTxId
        };

        return didDocumentMetadata;
    }
}