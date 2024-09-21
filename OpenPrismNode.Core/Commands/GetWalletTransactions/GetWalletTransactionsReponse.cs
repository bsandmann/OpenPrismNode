namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using Models;

public class GetWalletTransactionsReponse
{
    public int WalletEntityId { get; set; }
    public string WalletId { get; set; }
    public int WalletTransactionEntityId { get; set; }
    public long Fee { get; set; }
    public string TransactionId { get; set; }
    public int? OperationStatusEntityId { get; set; }
    public byte[]? OperationStatusId { get; set; }
    public byte[]? OperationHash { get; set; }
    public OperationTypeEnum? OperationType { get; set; }
    public OperationStatusEnum? Status { get; set; }
}