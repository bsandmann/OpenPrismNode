namespace OpenPrismNode.Core.Commands.ResolveDid.Transform;

using Models;
using Models.DidDocument;

public static class TransformToDidDocumentMetadata
{
    public static DidDocumentMetadata Transform(InternalDidDocument internalDidDocument, LedgerType ledger, DateTime? nextUpdate, bool includeNetworkIdentifier, bool isLongForm)
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
            NextUpdate = nextUpdate,
            CardanoTransactionPosition = isLongForm ? null : internalDidDocument.CardanoTransactionPosition,
            OperationPosition = isLongForm ? null : internalDidDocument.OperationPosition,
            OriginTxId = isLongForm ? null : internalDidDocument.OriginTxId,
            UpdateTxId = internalDidDocument.UpdateTxId,
            DeactivateTxId = internalDidDocument.DeactivateTxId
        };

        return didDocumentMetadata;
    }
}