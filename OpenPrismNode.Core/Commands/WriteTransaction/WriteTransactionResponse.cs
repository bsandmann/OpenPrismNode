namespace OpenPrismNode.Core.Commands.WriteTransaction;

using Models;

public class WriteTransactionResponse
{
    public byte[] OperationStatusId { get; init; }
    public OperationTypeEnum OperationType { get; init; }
    public string? DidSuffix { get; init; }
}