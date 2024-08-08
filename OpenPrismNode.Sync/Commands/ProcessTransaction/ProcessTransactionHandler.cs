namespace OpenPrismNode.Sync.Commands.ProcessTransaction;

using Core;
using DecodeTransaction;
using FluentResults;
using GetMetadataFromTransaction;
using GetPaymentDataFromTransaction;
using MediatR;
using Microsoft.Extensions.Logging;
using Models;
using ParseTransaction;

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

                    //
                    // var combinedWalletAddresses = new List<WalletAddress>();
                    // combinedWalletAddresses.AddRange(paymentdata.Value.Incoming.Select(p => p.WalletAddress));
                    // combinedWalletAddresses.AddRange(paymentdata.Value.Outgoing.Select(p => p.WalletAddress));
                    // var addressCreationResult = await _mediator.Send(new CreateWalletAddressesRequest(combinedWalletAddresses.Select(q => new PrismWalletAddressModel()
                    // {
                    //     StakeAddress = q.StakeAddress,
                    //     WalletAddressString = q.WalletAddressString
                    // }).ToList()));
                    var operationSequenceIndex = 0;
                    foreach (var operation in decodeResult.Value)
                    {
                        var resolveMode = new ResolveMode(request.Block.block_no, request.Transaction.block_index, operationSequenceIndex);
                        var parsingResult = await _mediator.Send(new ParseTransactionRequest(operation, operationSequenceIndex, resolveMode), cancellationToken);
                        if (parsingResult.IsSuccess)
                        {
                            try
                            {
                                _logger.LogInformation($"Parsing successful for transaction # {request.Transaction.block_index} in block # {request.Block.block_no}");
                                // var transactionRequest = new CreateTransactionRequest(
                                //     transactionHash: Hash.CreateFrom(blockTransaction.hash),
                                //     transactionBlock: Hash.CreateFrom(selectedBlock.hash),
                                //     transactionFee: long.Parse(blockTransaction.fee.ToString()),
                                //     transactionSize: blockTransaction.size,
                                //     transactionIndex: (uint)blockTransaction.block_index,
                                //     key: key,
                                //     parsingResult: parsingResult.Value,
                                //     incomingUtxos: paymentdata.Value.Incoming.Select(p => new PrismUtxoModel()
                                //     {
                                //         Index = (uint)p.Index,
                                //         Value = p.Value,
                                //         PrismWalletAddress = new PrismWalletAddressModel()
                                //         {
                                //             StakeAddress = p.WalletAddress.StakeAddress,
                                //             WalletAddressString = p.WalletAddress.WalletAddressString
                                //         }
                                //     }).ToList(),
                                //     outgoingUtxos: paymentdata.Value.Outgoing.Select(p => new PrismUtxoModel()
                                //     {
                                //         Index = (uint)p.Index,
                                //         Value = p.Value,
                                //         PrismWalletAddress = new PrismWalletAddressModel()
                                //         {
                                //             StakeAddress = p.WalletAddress.StakeAddress,
                                //             WalletAddressString = p.WalletAddress.WalletAddressString
                                //         }
                                //     }).ToList()
                                // );
                                // var transactionResult = await _mediator.Send(transactionRequest);
                                // if (transactionResult.IsFailed)
                                // {
                                //     _logger.LogError("Failed while writing transaction {TransactionId}: {Error}", blockTransaction.id, transactionResult.Errors.First().Message);
                                //     var transactionWritingErrorMessage = transactionResult.Errors.Single().Message;
                                //     continue;
                                // }
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
                            _logger.LogWarning($"Parsing error for transaction withd id '{request.Transaction.id}' in block-no '{request.Block.block_no}': {transactionWritingErrorMessage}");
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
            catch (Exception e)
            {
                _logger.LogError("Failed while parsing transaction {TransactionId}: {Error}", request.Transaction.id, e.Message);
            }
        }

        return Result.Ok();
    }
}