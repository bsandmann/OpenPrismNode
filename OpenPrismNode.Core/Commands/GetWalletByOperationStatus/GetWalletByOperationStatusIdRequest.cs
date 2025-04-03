namespace OpenPrismNode.Core.Commands.GetWalletByOperationStatus
{
    using FluentResults;
    using MediatR;
    using OpenPrismNode.Core.Commands.GetWallet;

    /// <summary>
    /// Looks up the wallet associated with a given OperationStatusId (byte array).
    /// </summary>
    public class GetWalletByOperationStatusIdRequest : IRequest<Result<GetWalletResponse?>>
    {
        public GetWalletByOperationStatusIdRequest(byte[] operationStatusId)
        {
            OperationStatusId = operationStatusId;
        }

        /// <summary>
        /// The "bytea" PK or unique hash for the OperationStatus entity.
        /// </summary>
        public byte[] OperationStatusId { get; }
    }
}