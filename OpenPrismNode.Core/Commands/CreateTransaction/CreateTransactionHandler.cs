namespace OpenPrismNode.Core.Commands.CreateTransaction;

using CreateTransactionCreateDid;
using CreateTransactionDeactivateDid;
using CreateTransactionUpdateDid;
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
                prismServices: request.ParsingResult.AsCreateDid().didDocument.PrismServices.ToList(),
                patchedContexts: request.ParsingResult.AsCreateDid().didDocument.Contexts.ToList()
            ), cancellationToken);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }
        else if (request.ParsingResult.OperationResultType == OperationResultType.UpdateDid)
        {
            var result = await this._mediator.Send(new CreateTransactionUpdateDidRequest(
                transactionHash: request.TransactionHash,
                blockHash: request.BlockHash,
                blockHeight: request.BlockHeight,
                fees: request.TransactionFees,
                size: request.TransactionSize,
                index: request.TransactionIndex,
                operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsUpdateDid().operationBytes),
                previousOperationHash: request.ParsingResult.AsUpdateDid().previousOperationHash,
                did: request.ParsingResult.AsUpdateDid().didIdentifier,
                signingKeyId: request.ParsingResult.AsUpdateDid().signingKeyId,
                updateDidActions: request.ParsingResult.AsUpdateDid().updateDidActionResults,
                operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
                utxos: request.Utxos), cancellationToken);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }
        else if (request.ParsingResult.OperationResultType == OperationResultType.DeactivateDid)
        {
            var result = await this._mediator.Send(new CreateTransactionDeactivateDidRequest(
                transactionHash: request.TransactionHash,
                blockHash: request.BlockHash,
                blockHeight: request.BlockHeight,
                fees: request.TransactionFees,
                size: request.TransactionSize,
                index: request.TransactionIndex,
                operationHash: new Hash(_sha256Service).Of(request.ParsingResult.AsUpdateDid().operationBytes),
                previousOperationHash: request.ParsingResult.AsUpdateDid().previousOperationHash,
                did: request.ParsingResult.AsUpdateDid().didIdentifier,
                signingKeyId: request.ParsingResult.AsUpdateDid().signingKeyId,
                operationSequenceNumber: request.ParsingResult.OperationSequenceNumber,
                utxos: request.Utxos), cancellationToken);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }
        //TODO procotol version update?
        else
        {
            return Result.Fail("Invalid operation type");
        }

        return Result.Ok();
    }
}