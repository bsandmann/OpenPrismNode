namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using Core;
using Core.Commands.CreateAddresses;
using Core.Commands.CreateTransaction;
using Core.Models;
using DecodeTransaction;
using FluentResults;
using GetMetadataFromTransaction;
using GetPaymentDataFromTransaction;
using MediatR;
using Microsoft.Extensions.Logging;
using Models;
using ParseTransaction;
using PostgresModels;

public class ProcessTransactionHandler : IRequestHandler<ProcessTransactionRequest, Result>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessTransactionHandler> _logger;

    public ProcessTransactionHandler(IMediator mediator, ILogger<ProcessTransactionHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }


    public async Task<Result> Handle(ProcessTransactionRequest request, CancellationToken cancellationToken)
    {
        var metadata = await _mediator.Send(new GetMetadataFromTransactionRequest(request.Transaction.id, PrismParameters.MetadataKey), cancellationToken);
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
                    var paymentdata = await _mediator.Send(new GetPaymentDataFromTransactionRequest(request.Transaction.id), cancellationToken);
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
                        var parsingResult = await _mediator.Send(new ParseTransactionRequest(operation, operationSequenceIndex, resolveMode), cancellationToken);
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

                            if (parsingResult.Value.OperationResultType == OperationResultType.CreateDid
                                && parsingResult.Value.AsCreateDid().didDocument.PublicKeys.Any() &&
                                parsingResult.Value.AsCreateDid().didDocument.PublicKeys.Any(p => p.Curve != PrismParameters.Secp256k1CurveName))
                            {
                                throw new Exception($"Found other crypt alg on {request.Block.block_no}");
                            }

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
                                _logger.LogInformation($"Unsupported operation in block-no '{request.Block.block_no}'");
                            }
                            else
                            {
                                _logger.LogWarning($"Parsing error for transaction withd id '{request.Transaction.id}' in block-no '{request.Block.block_no}': {transactionWritingErrorMessage}");
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
                            // OUT OF SCOPE

                            // var resolvedDid = await _mediator.Send(new ResolveDidRequest(new Did(did)));
                            // if (resolvedDid.IsFailed)
                            // {
                            //     _logger.LogCritical("Failed while resolving did {Did}: {Error}", did, resolvedDid.Errors.First().Message);
                            // }
                            // else
                            // {
                            //     var createLedgerResult = await _mediator.Send(new CreateLederRequest("prism", _appSettings.PrismNetwork.Name.ToLowerInvariant()));
                            //     if (createLedgerResult.IsFailed)
                            //     {
                            //         _logger.LogCritical("Failed while creating ledger {Ledger}: {Error}", "prism", createLedgerResult.Errors.First().Message);
                            //         continue;
                            //     }
                            //
                            //     var createDidResult = await _mediator.Send(new CreateEntryInDidStorageRequest(
                            //         resolveDidResponse: resolvedDid.Value,
                            //         network: _appSettings.PrismNetwork.Name.ToLowerInvariant(),
                            //         operationResultType: parsingResult.Value.OperationResultType,
                            //         transactionHash: Hash.CreateFrom(blockTransaction.hash),
                            //         transactionBlock: Hash.CreateFrom(selectedBlock.hash),
                            //         blockheight: selectedBlock.block_no,
                            //         blocktime: selectedBlock.time,
                            //         transactionSize: blockTransaction.size,
                            //         transactionFees: long.Parse(blockTransaction.fee.ToString(CultureInfo.InvariantCulture)),
                            //         transactionIndex: (uint)blockTransaction.block_index,
                            //         incomingUtxos: paymentdata.Value.Incoming.Select(p => new PrismUtxoModel()
                            //         {
                            //             Index = (uint)p.Index,
                            //             Value = p.Value,
                            //             PrismWalletAddress = new PrismWalletAddressModel()
                            //             {
                            //                 StakeAddress = p.WalletAddress.StakeAddress,
                            //                 WalletAddressString = p.WalletAddress.WalletAddressString
                            //             }
                            //         }).ToList(),
                            //         outgoingUtxos: paymentdata.Value.Outgoing.Select(p => new PrismUtxoModel()
                            //         {
                            //             Index = (uint)p.Index,
                            //             Value = p.Value,
                            //             PrismWalletAddress = new PrismWalletAddressModel()
                            //             {
                            //                 StakeAddress = p.WalletAddress.StakeAddress,
                            //                 WalletAddressString = p.WalletAddress.WalletAddressString
                            //             }
                            //         }).ToList()
                            //     ));
                            //
                            //     if (createDidResult.IsFailed)
                            //     {
                            //         _logger.LogError("Failed while creating did {Did}: {Error}", did, createDidResult.Errors.First().Message);
                            //     }
                            // }
                        }

                        operationSequenceIndex++;
                    }

                    _logger.LogInformation($"Successfully processed block {request.Block.block_no}");
                }
            }
            catch
                (Exception e)
            {
                _logger.LogError("Failed while parsing transaction {TransactionId}: {Error}", request.Transaction.id, e.Message);
            }
        }

        return Result.Ok();
    }
}