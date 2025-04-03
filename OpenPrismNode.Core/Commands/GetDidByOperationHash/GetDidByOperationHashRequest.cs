namespace OpenPrismNode.Core.Commands.GetDidByOperationHash
{
    using FluentResults;
    using MediatR;
    using Models;

    public class GetDidByOperationHashRequest : IRequest<Result<string>>
    {
        public GetDidByOperationHashRequest(byte[] operationHash, OperationTypeEnum operationType)
        {
            OperationHash = operationHash;
            OperationType = operationType;
        }

        /// <summary>
        /// The OperationHash by which we will look up the DID.
        /// </summary>
        public byte[] OperationHash { get; }

        public OperationTypeEnum OperationType { get; }
    }
}