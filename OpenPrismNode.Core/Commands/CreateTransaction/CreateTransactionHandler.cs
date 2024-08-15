namespace OpenPrismNode.Core.Commands.CreateTransaction;

using CreateTransactionCreateDid;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;

/// <summary>
/// Handler to create new transactions inside the node-database
/// it utilizes the different sub-commands to write the specifics of each PRISM-operation type
/// </summary>
public class CreateTransactionHandler : IRequestHandler<CreateTransactionRequest, Result>
{
    private readonly IMediator _mediator;
    private readonly ISha256Service _sha256Service;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="sha256Service"></param>
    public CreateTransactionHandler(IMediator mediator, ISha256Service sha256Service)
    {
        _mediator = mediator;
        _sha256Service = sha256Service;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.ParsingResult.OperationResultType == OperationResultType.CreateDid)
        {
            // Before writing the operation to the database we verify the signature of the signedPrismOperation
            // in case of the create Operation this happens in the parsing process, because the corresponding public key
            // is always available
            var result = await this._mediator.Send(new CreateTransactionCreateDidRequest(
                transactionHash: request.TransactionHash,
                blockHash: request.BlockHash,
                blockHeight: request.BlockHeight,
                fees: request.TransactionFees,
                size: request.TransactionSize,
                index: request.TransactionIndex,
                operationHash: Hash.CreateFrom(PrismEncoding.HexToByteArray(request.ParsingResult.AsCreateDid().didDocument.DidIdentifier)),
                did: request.ParsingResult.AsCreateDid().didDocument.DidIdentifier,
                signingKeyId: request.ParsingResult.AsCreateDid().signingKeyId,
                operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
                utxos: request.Utxos,
                prismPublicKeys: request.ParsingResult.AsCreateDid().didDocument.PublicKeys.ToList(),
                prismServices: request.ParsingResult.AsCreateDid().didDocument.PrismServices.ToList()), cancellationToken);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }
        // else if (request.ParsingResult.OperationResultType == OperationResultType.UpdateDid)
        // {
        //     // Before writing the operation to the database we verify the signature of the signedPrismOperation
        //     // When going from the past to the present the corresponding public key should already be in the database
        //     var result = await this._mediator.Send(new CreateTransactionUpdateDidRequest(
        //         transactionHash: request.TransactionHash,
        //         prismBlockHash: request.TransactionBlock,
        //         fees: request.TransactionFees,
        //         size: request.TransactionSize,
        //         index: request.TransactionIndex,
        //         label: request.Key,
        //         operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsUpdateDid().operationBytes),
        //         previousOperationHash: request.ParsingResult.AsUpdateDid().previousOperationHash,
        //         did: request.ParsingResult.AsUpdateDid().did.Identifier,
        //         signingKeyId: request.ParsingResult.AsUpdateDid().signingKeyId,
        //         updateDidActions: request.ParsingResult.AsUpdateDid().updateDidActionResults,
        //         operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
        //         incomingUtxos: request.IncomingUtxos,
        //         outgoingUtxos: request.OutgoingUtxos), cancellationToken);
        //     if (result.IsFailed)
        //     {
        //         return Result.Fail(result.Errors);
        //     }
        // }
        // else if (request.ParsingResult.OperationResultType == OperationResultType.IssueCredentialBatch)
        // {
        //     // Before writing the operation to the database we verify the signature of the signedPrismOperation
        //     // When going from the past to the present the corresponding public key should already be in the database
        //     var result = await this._mediator.Send(new CreateTransactionIssueCredentialBatchRequest(
        //         transactionHash: request.TransactionHash,
        //         prismBlockHash: request.TransactionBlock,
        //         fees: request.TransactionFees,
        //         size: request.TransactionSize,
        //         index: request.TransactionIndex,
        //         label: request.Key,
        //         operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsIssueCredentialBatch().operationBytes),
        //         did: request.ParsingResult.AsIssueCredentialBatch().did.Identifier,
        //         signingKeyId: request.ParsingResult.AsIssueCredentialBatch().signingKeyId,
        //         merkleRoot: request.ParsingResult.AsIssueCredentialBatch().merkleRoot,
        //         operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
        //         credentialBatchId: request.ParsingResult.AsIssueCredentialBatch().credentialBatchId,
        //         incomingUtxos: request.IncomingUtxos,
        //         outgoingUtxos: request.OutgoingUtxos), cancellationToken);
        //     if (result.IsFailed)
        //     {
        //         return Result.Fail(result.Errors);
        //     }
        // }
        // else if (request.ParsingResult.OperationResultType == OperationResultType.RevokeCredentials)
        // {
        //     // Before writing the operation to the database we verify the signature of the signedPrismOperation
        //     // When going from the past to the present the corresponding public key should already be in the database
        //     var result = await this._mediator.Send(new CreateTransactionRevokeCredentialsRequest(
        //         transactionHash: request.TransactionHash,
        //         prismBlockHash: request.TransactionBlock,
        //         fees: request.TransactionFees,
        //         size: request.TransactionSize,
        //         index: request.TransactionIndex,
        //         label: request.Key,
        //         operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsRevokeCredentials().operationBytes),
        //         credentialBatchId: request.ParsingResult.AsRevokeCredentials().credentialBatchId,
        //         signingKeyId: request.ParsingResult.AsRevokeCredentials().signingKeyId,
        //         previousOperationHash: request.ParsingResult.AsRevokeCredentials().previousOperationHash,
        //         credentialsToBeRevokedList: request.ParsingResult.AsRevokeCredentials().hashOfCredentialsToBeRevokedList,
        //         operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
        //         incomingUtxos: request.IncomingUtxos,
        //         outgoingUtxos: request.OutgoingUtxos), cancellationToken);
        //     if (result.IsFailed)
        //     {
        //         return Result.Fail(result.Errors);
        //     }
        // }
        // else if (request.ParsingResult.OperationResultType == OperationResultType.ProtocolVersionUpdate)
        // {
        //     var result = await this._mediator.Send(new CreateTransactionProtocolVersionUpdateRequest(
        //         transactionHash: request.TransactionHash,
        //         prismBlockHash: request.TransactionBlock,
        //         fees: request.TransactionFees,
        //         size: request.TransactionSize,
        //         index: request.TransactionIndex,
        //         label: request.Key,
        //         operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsProtocolVersionUpdate().operationBytes),
        //         signingKeyId: request.ParsingResult.AsProtocolVersionUpdate().signingKeyId,
        //         effectiveSinceBlock: request.ParsingResult.AsProtocolVersionUpdate().prismProtocolUpdateVersion.EffectiveSinceBlock,
        //         minorVersion: request.ParsingResult.AsProtocolVersionUpdate().prismProtocolUpdateVersion.PrismProtocolVersion.MinorVersion,
        //         majorVersion: request.ParsingResult.AsProtocolVersionUpdate().prismProtocolUpdateVersion.PrismProtocolVersion.MajorVersion,
        //         versionName: request.ParsingResult.AsProtocolVersionUpdate().prismProtocolUpdateVersion.VersionName,
        //         operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
        //         incomingUtxos: request.IncomingUtxos,
        //         outgoingUtxos: request.OutgoingUtxos), cancellationToken);
        //     if (result.IsFailed)
        //     {
        //         return Result.Fail(result.Errors);
        //     }
        // }
        // else if (request.ParsingResult.OperationResultType == OperationResultType.DeactivateDid)
        // {
        //     // Before writing the operation to the database we verify the signature of the signedPrismOperation
        //     // When going from the past to the present the corresponding public key should already be in the database
        //     var result = await this._mediator.Send(new CreateTransactionDeactivateDidRequest(
        //         transactionHash: request.TransactionHash,
        //         prismBlockHash: request.TransactionBlock,
        //         fees: request.TransactionFees,
        //         size: request.TransactionSize,
        //         index: request.TransactionIndex,
        //         label: request.Key,
        //         operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsDeactivateDid().operationBytes),
        //         previousOperationHash: request.ParsingResult.AsDeactivateDid().previousOperationHash,
        //         did: request.ParsingResult.AsDeactivateDid().deactivatedDid.Identifier,
        //         signingKeyId: request.ParsingResult.AsDeactivateDid().signingKeyId,
        //         operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
        //         incomingUtxos: request.IncomingUtxos,
        //         outgoingUtxos: request.OutgoingUtxos), cancellationToken);
        //     if (result.IsFailed)
        //     {
        //         return Result.Fail(result.Errors);
        //     }
        // }
        else
        {
            return Result.Fail("Invalid operation type");
        }

        return Result.Ok();
    }
}