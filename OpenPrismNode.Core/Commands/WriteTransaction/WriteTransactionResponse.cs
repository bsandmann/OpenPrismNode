namespace OpenPrismNode.Core.Commands.WriteTransaction;

using Models;

public class WriteTransactionResponse
{
    public byte[] OperationStatusId { get; set; }
    public OperationTypeEnum OperationType { get; set; }
    public string? DidSuffix { get; set; }
}