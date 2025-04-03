namespace OpenPrismNode.Core.Commands.GetVerificationMethodSecrets
{
    using System.Collections.Generic;
    using FluentResults;
    using MediatR;

    public class GetVerificationMethodSecretsRequest : IRequest<Result<List<Models.VerificationMethodSecret>>>
    {
        public GetVerificationMethodSecretsRequest(byte[] operationStatusId)
        {
            OperationStatusId = operationStatusId;
        }

        /// <summary>
        /// The byte array that uniquely identifies the OperationStatus.
        /// </summary>
        public byte[] OperationStatusId { get; }
    }
}