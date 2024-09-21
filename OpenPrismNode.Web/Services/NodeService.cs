namespace OpenPrismNode.Web.Services
{
    using Core.Commands.CreateBlock;
    using Core.Commands.CreateOperationsStatus;
    using Core.Commands.CreateTransaction;
    using Core.Commands.GetMostRecentBlock;
    using Core.Commands.GetOperationStatus;
    using Core.Common;
    using Core.Crypto;
    using Core.Models;
    using FluentResults;
    using global::Grpc.Core;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using MediatR;
    using Microsoft.Extensions.Options;
    using OpenPrismNodeService;
    using Sync;
    using Sync.Commands.ParseTransaction;
    using System;
    using Core.Commands.GetWallets;
    using Core.Commands.ResolveDid;
    using Core.Commands.ResolveDid.Transform;
    using Core.Commands.WriteTransaction;
    using Core.Parser;
    using Enum = System.Enum;

    public class NodeService : OpenPrismNodeService.NodeService.NodeServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<NodeService> _logger;
        private readonly ISha256Service _sha256Service;
        private readonly IOptions<AppSettings> _appSettings;

        public NodeService(IMediator mediator, ILogger<NodeService> logger, ISha256Service sha256Service, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _mediator = mediator;
            _sha256Service = sha256Service;
            _appSettings = appSettings;
        }

        public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HealthCheckResponse());
        }

        public override async Task<GetDidDocumentResponse> GetDidDocument(GetDidDocumentRequest request, ServerCallContext context)
        {
            var did = request.Did;
            var ledger = _appSettings.Value.PrismLedger.Name;
            var isParseableLedger = Enum.TryParse<LedgerType>(ledger.Equals("inmemory", StringComparison.InvariantCultureIgnoreCase) ? "inmemory" : "cardano" + ledger, ignoreCase: true, out var ledgerQueryType);
            if (!isParseableLedger)
            {
                return new GetDidDocumentResponse();
            }

            if (!DidUrlParser.TryParse(did, out var parsedDid))
            {
                return new GetDidDocumentResponse();
            }

            var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerQueryType, parsedDid.MethodSpecificId, null, null, null));
            if (resolveResult.IsFailed)
            {
                return new GetDidDocumentResponse();
            }

            var transformedResult = TransformToPrismGrpcResponse.Transform(resolveResult.Value.InternalDidDocument);
            return transformedResult;
        }

        public override async Task<GetOperationInfoResponse> GetOperationInfo(GetOperationInfoRequest request, ServerCallContext context)
        {
            if (request.OperationId != null)
            {
                var operationStatusId = PrismEncoding.ByteStringToByteArray(request.OperationId);

                var operationIdResult = await _mediator.Send(new GetOperationStatusRequest(operationStatusId));
                if (operationIdResult.IsFailed)
                {
                    return new GetOperationInfoResponse()
                    {
                        Details = "Operation could not be found",
                        OperationStatus = OperationStatus.UnknownOperation,
                        TransactionId = "0",
                        LastSyncedBlockTimestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                    };
                }

                return new GetOperationInfoResponse()
                {
                    Details = "some details",
                    OperationStatus = MapOperationStatus(operationIdResult.Value!.Status),
                    TransactionId = operationIdResult.Value.TransactionId,
                    LastSyncedBlockTimestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
            }
            else
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Operation not found"));
            }
        }

        private OperationStatus MapOperationStatus(OperationStatusEnum status)
        {
            return status switch
            {
                OperationStatusEnum.UnknownOperation => OperationStatus.UnknownOperation,
                OperationStatusEnum.PendingSubmission => OperationStatus.PendingSubmission,
                OperationStatusEnum.AwaitConfirmation => OperationStatus.AwaitConfirmation,
                OperationStatusEnum.ConfirmedAndApplied => OperationStatus.ConfirmedAndApplied,
                OperationStatusEnum.ConfirmedAndRejected => OperationStatus.ConfirmedAndRejected,
                _ => throw new NotImplementedException()
            };
        }

        public override async Task<ScheduleOperationsResponse> ScheduleOperations(ScheduleOperationsRequest request, ServerCallContext context)
        {
            if (request.SignedOperations.Count != 1)
            {
                return GenerateScheduleOperationsErrorResponse(Hash.CreateRandom().Value, new List<IError>() { new Error("A single SignedAtalaOperation is expected") });
            }

            var isInMemoryLedger = _appSettings.Value.PrismLedger.Name.Equals("inMemory", StringComparison.InvariantCultureIgnoreCase);
            if (isInMemoryLedger)
            {
                var operationStatusId = _sha256Service.HashData(request.SignedOperations[0].ToByteArray());
                var parsingResult = await _mediator.Send(new ParseTransactionRequest(request.SignedOperations[0], LedgerType.InMemory, 0, new ResolveMode(null, null, null)));
                if (parsingResult.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(operationStatusId, parsingResult.Errors);
                }

                var mostRecentInMemoryBlock = await _mediator.Send(new GetMostRecentBlockRequest(LedgerType.InMemory));
                if (mostRecentInMemoryBlock.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(operationStatusId, mostRecentInMemoryBlock.Errors);
                }

                var newBlock = await _mediator.Send(new CreateBlockRequest(LedgerType.InMemory, Hash.CreateRandom(), Hash.CreateFrom(mostRecentInMemoryBlock.Value.BlockHash), mostRecentInMemoryBlock.Value.BlockHeight + 1, mostRecentInMemoryBlock.Value.BlockHeight, 1, DateTime.UtcNow, 0, false),
                    CancellationToken.None);
                if (newBlock.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(operationStatusId, newBlock.Errors);
                }

                var transactionRequest = new CreateTransactionRequest(
                    transactionHash: Hash.CreateRandom(),
                    blockHash: Hash.CreateFrom(newBlock.Value.BlockHash),
                    blockHeight: newBlock.Value.BlockHeight,
                    transactionFee: 0,
                    transactionSize: 0,
                    transactionIndex: 0,
                    parsingResult: parsingResult.Value,
                    utxos: new List<UtxoWrapper>()
                );
                var transactionResult = await _mediator.Send(transactionRequest, new CancellationToken());
                if (transactionResult.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(operationStatusId, transactionResult.Errors);
                }

                byte[]? operationHash = null;
                OperationTypeEnum operationType = OperationTypeEnum.CreateDid;
                if (parsingResult.Value.OperationResultType == OperationResultType.CreateDid)
                {
                    operationHash = PrismEncoding.HexToByteArray(parsingResult.Value.AsCreateDid().didDocument.DidIdentifier);
                    operationType = OperationTypeEnum.CreateDid;
                }
                else if (parsingResult.Value.OperationResultType == OperationResultType.UpdateDid)
                {
                    operationHash = parsingResult.Value.AsUpdateDid().operationBytes;
                    operationType = OperationTypeEnum.UpdateDid;
                }
                else if (parsingResult.Value.OperationResultType == OperationResultType.DeactivateDid)
                {
                    operationHash = parsingResult.Value.AsDeactivateDid().operationBytes;
                    operationType = OperationTypeEnum.DeactivateDid;
                }
                else
                {
                    throw new NotImplementedException();
                }

                var createOperationStatusResult = await _mediator.Send(new CreateOperationStatusRequest(
                    operationStatusId,
                    operationHash,
                    OperationStatusEnum.ConfirmedAndApplied,
                    operationType
                ));
                if (createOperationStatusResult.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(operationStatusId, transactionResult.Errors);
                }

                operationStatusId = createOperationStatusResult.Value.OperationStatusId;

                return new ScheduleOperationsResponse()
                {
                    Outputs =
                    {
                        new OperationOutput()
                        {
                            OperationId = PrismEncoding.ByteArrayToByteString(operationStatusId),
                            CreateDidOutput = operationType == OperationTypeEnum.CreateDid
                                ? new CreateDIDOutput()
                                {
                                    DidSuffix = parsingResult.Value.AsCreateDid().didDocument.DidIdentifier
                                }
                                : null,
                            UpdateDidOutput = operationType == OperationTypeEnum.UpdateDid
                                ? new UpdateDIDOutput()
                                {
                                }
                                : null,
                            DeactivateDidOutput = operationType == OperationTypeEnum.DeactivateDid
                                ? new DeactivateDIDOutput()
                                {
                                }
                                : null
                        }
                    }
                };
            }
            else
            {
                // TODO: since we cannot distinguish between different wallets, we are using the first available wallet
                // What we need here is to read the X-API-KEY header and get the walletId from the header
                // The problem is that the current identus-implementations does not allow header information

                var getWalletsResult = await _mediator.Send(new GetWalletsRequest());
                if (getWalletsResult.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(Hash.CreateRandom().Value, getWalletsResult.Errors);
                }

                if (!getWalletsResult.Value.Any())
                {
                    return GenerateScheduleOperationsErrorResponse(Hash.CreateRandom().Value, new List<IError>() { new Error("No wallets found") });
                }

                var selectedWallet = getWalletsResult.Value.MaxBy(p => p.Balance);

                var transactionResult = await _mediator.Send(new WriteTransactionRequest()
                {
                    WalletId = selectedWallet.WalletId,
                    SignedAtalaOperation = request.SignedOperations[0]
                });

                if (transactionResult.IsFailed)
                {
                    return GenerateScheduleOperationsErrorResponse(transactionResult.Value.OperationStatusId, transactionResult.Errors);
                }

                return new ScheduleOperationsResponse()
                {
                    Outputs =
                    {
                        new OperationOutput()
                        {
                            OperationId = PrismEncoding.ByteArrayToByteString(transactionResult.Value.OperationStatusId),
                            CreateDidOutput = transactionResult.Value.OperationType == OperationTypeEnum.CreateDid
                                ? new CreateDIDOutput()
                                {
                                    DidSuffix = transactionResult.Value.DidSuffix
                                }
                                : null,
                            UpdateDidOutput = transactionResult.Value.OperationType == OperationTypeEnum.UpdateDid
                                ? new UpdateDIDOutput()
                                {
                                }
                                : null,
                            DeactivateDidOutput = transactionResult.Value.OperationType == OperationTypeEnum.DeactivateDid
                                ? new DeactivateDIDOutput()
                                {
                                }
                                : null
                        }
                    }
                };
            }
        }

        private ScheduleOperationsResponse GenerateScheduleOperationsErrorResponse(byte[] operationStatusId, List<IError> errors)
        {
            return new ScheduleOperationsResponse()
            {
                Outputs =
                {
                    new OperationOutput()
                    {
                        OperationId = PrismEncoding.ByteArrayToByteString(operationStatusId),
                        Error = string.IsNullOrEmpty(errors.FirstOrDefault()?.Message) ? "Operation failed" : errors.FirstOrDefault().Message
                    }
                }
            };
        }
    }
}