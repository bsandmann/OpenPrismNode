namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using Models;

public class GetWalletTransactionsReponse
{
    public int WalletEntityId { get; init; }
    public string WalletId { get; init; }
    public int WalletTransactionEntityId { get; init; }
    public long Fee { get; init; }
    public string TransactionId { get; init; }
    public int? OperationStatusEntityId { get; init; }
    public byte[]? OperationStatusId { get; init; }
    public byte[]? OperationHash { get; init; }
    public OperationTypeEnum? OperationType { get; init; }
    public OperationStatusEnum? Status { get; init; }
}