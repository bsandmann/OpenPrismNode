namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using Core;
using Core.Commands.CreateAddresses;
using Core.Commands.CreateTransaction;
using Core.Common;
using Core.Models;
using DecodeTransaction;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ParseTransaction;
using Services;
using Abstractions;

public class ProcessTransactionHandler : IRequestHandler<ProcessTransactionRequest, Result>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessTransactionHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IIngestionService _ingestionService;
    private readonly ITransactionProvider _transactionProvider;

    public ProcessTransactionHandler(
        IMediator mediator, 
        ILogger<ProcessTransactionHandler> logger, 
        IOptions<AppSettings> appSettings, 
        IIngestionService ingestionService,
        ITransactionProvider transactionProvider)
    {
        _mediator = mediator;
        _logger = logger;
        _appSettings = appSettings.Value;
        _ingestionService = ingestionService;
        _transactionProvider = transactionProvider;
    }

    public async Task<Result> Handle(ProcessTransactionRequest request, CancellationToken cancellationToken)
    {
        var metadata = await _transactionProvider.GetMetadataFromTransaction(request.Transaction.id, request.Transaction.hash, _appSettings.MetadataKey, cancellationToken);
        if (metadata.IsFailed)
        {
            _logger.LogError($"Failed while reading metadata of transaction # {request.Transaction.block_index} in block # {request.Block.block_no}: {metadata.Errors.First().Message}");
        }

        if (metadata.IsSuccess)
        {
            var jsonMetadata = metadata.Value.json;
            try
            {
                var decodeResult = await _mediator.Send(new DecodeTransactionRequest(jsonMetadata), cancellationToken);
                if (decodeResult.IsFailed)
                {
                    var parsingErrorMessage = decodeResult.Errors.SingleOrDefault()?.Message;
                    if (string.IsNullOrEmpty(parsingErrorMessage) || parsingErrorMessage.Equals(ParserErrors.UnableToDeserializeBlockchainMetadata))
                    {
                        // This happens if the metadata is not PRISM related, but still has the PRISM key
                        // Unsual, but can happen. Also happens for initial PRISM operations
                    }
                    else
                    {
                        _logger.LogError($"Failed while parsing transaction # {request.Transaction.block_index} in block # {request.Block.block_no}: {parsingErrorMessage}");
                    }
                }
                else if (decodeResult.IsSuccess)
                {
                    // Create all wallet-addresses upfront
                    var paymentdata = await _transactionProvider.GetPaymentDataFromTransaction(request.Transaction.id, cancellationToken);
                    if (paymentdata.IsFailed)
                    {
                        _logger.LogError($"Failed while reading payment data of transaction # {request.Transaction.block_index} in block # {request.Block.block_no}: {paymentdata.Errors.First().Message}");
                    }

                    var combinedWalletAddresses = new List<WalletAddress>();
                    combinedWalletAddresses.AddRange(paymentdata.Value.Incoming.Select(p => p.WalletAddress));
                    combinedWalletAddresses.AddRange(paymentdata.Value.Outgoing.Select(p => p.WalletAddress));

                    var addressCreationResult = await _mediator.Send(new CreateAddressesRequest(combinedWalletAddresses), cancellationToken);
                    if (addressCreationResult.IsFailed)
                    {
                        _logger.LogError($"Failed while creating wallet addresses for transaction # {request.Transaction.block_index} in block # {request.Block.block_no}: {addressCreationResult.Errors.First().Message}");
                    }

                    var operationSequenceIndex = 0;
                    foreach (var operation in decodeResult.Value)
                    {
                        var resolveMode = new ResolveMode(request.Block.block_no, request.Transaction.block_index, operationSequenceIndex);
                        var parsingResult = await _mediator.Send(new ParseTransactionRequest(operation, request.Ledger, operationSequenceIndex, resolveMode), cancellationToken);
                        if (parsingResult.IsSuccess)
                        {
                            var utxos = paymentdata.Value.Incoming
                                .Select(p => new UtxoWrapper
                                {
                                    Index = p.Index,
                                    Value = (int)p.Value,
                                    IsOutgoing = false,
                                    WalletAddress = new WalletAddress
                                    {
                                        StakeAddressString = p.WalletAddress.StakeAddressString,
                                        WalletAddressString = p.WalletAddress.WalletAddressString
                                    }
                                })
                                .Concat(paymentdata.Value.Outgoing.Select(p => new UtxoWrapper
                                {
                                    Index = p.Index,
                                    Value = (int)p.Value,
                                    IsOutgoing = true,
                                    WalletAddress = new WalletAddress
                                    {
                                        StakeAddressString = p.WalletAddress.StakeAddressString,
                                        WalletAddressString = p.WalletAddress.WalletAddressString
                                    }
                                }))
                                .ToList();

                            try
                            {
                                _logger.LogInformation($"Parsing successful for transaction # {request.Transaction.block_index} in block # {request.Block.block_no}");
                                var transactionRequest = new CreateTransactionRequest(
                                    transactionHash: Hash.CreateFrom(request.Transaction.hash),
                                    blockHash: Hash.CreateFrom(request.Block.hash),
                                    blockHeight: request.Block.block_no,
                                    transactionFee: (int)request.Transaction.fee,
                                    transactionSize: request.Transaction.size,
                                    transactionIndex: request.Transaction.block_index,
                                    parsingResult: parsingResult.Value,
                                    utxos: utxos
                                );
                                var transactionResult = await _mediator.Send(transactionRequest, cancellationToken);
                                if (transactionResult.IsFailed)
                                {
                                    _logger.LogError($"Failed while writing transaction #{transactionRequest.TransactionIndex} on block {transactionRequest.BlockHeight}: {transactionResult.Errors.First().Message}");
                                    continue;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"Unable to for transaction # {request.Transaction.block_index} in block # {request.Block.block_no}: {e.Message} {e.InnerException?.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            var transactionWritingErrorMessage = parsingResult.Errors.Single().Message;
                            if (transactionWritingErrorMessage == ParserErrors.UnsupportedOperation)
                            {
                                _logger.LogInformation($"Unsupported operation in block # '{request.Block.block_no}'");
                            }
                            else
                            {
                                _logger.LogWarning($"Parsing error for transaction withd id '{request.Transaction.id}' in block # '{request.Block.block_no}': {transactionWritingErrorMessage}");
                            }

                            continue;
                        }

                        string? didIdentifier = null;
                        if (parsingResult.Value.OperationResultType == OperationResultType.CreateDid)
                        {
                            didIdentifier = parsingResult.Value.AsCreateDid().didDocument.DidIdentifier;
                        }
                        else if (parsingResult.Value.OperationResultType == OperationResultType.UpdateDid)
                        {
                            didIdentifier = parsingResult.Value.AsUpdateDid().didIdentifier;
                        }
                        else if (parsingResult.Value.OperationResultType == OperationResultType.DeactivateDid)
                        {
                            didIdentifier = parsingResult.Value.AsDeactivateDid().deactivatedDid;
                        }
                        else if (parsingResult.Value.OperationResultType == OperationResultType.ProtocolVersionUpdate)
                        {
                            throw new Exception($"Encoutered ProtocolVersionUpdate on BlockNumber: {request.Block.block_no}");
                        }

                        if (didIdentifier is not null)
                        {
                            await _ingestionService.Ingest(didIdentifier, request.Ledger);
                        }

                        operationSequenceIndex++;
                    }

                    _logger.LogInformation($"Successfully processed block {request.Block.block_no}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed while parsing transaction {request.Transaction.id} in Block #: {request.Block.block_no}: {e.Message}");
            }
        }

        return Result.Ok();
    }
}