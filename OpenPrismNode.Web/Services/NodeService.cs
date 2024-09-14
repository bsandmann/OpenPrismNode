namespace OpenPrismNode.Web.Services
{
    using global::Grpc.Core;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using OpenPrismNodeService;

    public class NodeService: OpenPrismNodeService.NodeService.NodeServiceBase
    {
        private readonly ILogger<NodeService> _logger;

        public NodeService(ILogger<NodeService> logger)
        {
            _logger = logger;
        }
        
        public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HealthCheckResponse());
        }
        
        public override Task<GetDidDocumentResponse> GetDidDocument(GetDidDocumentRequest request, ServerCallContext context)
        {
            // Implement the logic to retrieve and return the DID document
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Method not implemented."));
        }

        public override Task<GetOperationInfoResponse> GetOperationInfo(GetOperationInfoRequest request, ServerCallContext context)
        {
            if (request.OperationId != null)
            {
                var response = new GetOperationInfoResponse()
                {
                    Details = "some details",
                    OperationStatus = OperationStatus.ConfirmedAndApplied,
                    TransactionId = "123",
                    LastSyncedBlockTimestamp = new Timestamp() { Seconds = 123, Nanos = 123 }
                };
                return Task.FromResult(response);
            }
            else
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Operation not found"));
            }
        }

        public override Task<ScheduleOperationsResponse> ScheduleOperations(ScheduleOperationsRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Operation not found"));
            if (request.SignedOperations.Count != 1)
            {
                throw new Exception();
            }

            if (request.SignedOperations[0].Operation.OperationCase == AtalaOperation.OperationOneofCase.CreateDid)
            {
                // create
            }
            else if (request.SignedOperations[0].Operation.OperationCase == AtalaOperation.OperationOneofCase.UpdateDid)
            {
                // update
            }
            else if (request.SignedOperations[0].Operation.OperationCase == AtalaOperation.OperationOneofCase.DeactivateDid)
            {
                // deactivate
            }
            else
            {
                throw new ApplicationException();
            }

            var response = new ScheduleOperationsResponse
            {
                Outputs =
                {
                    new OperationOutput()
                    {
                        OperationId = ByteString.FromBase64("asdf")
                    },
                },
            };
            return Task.FromResult(response);
        }
    }
}