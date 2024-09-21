namespace OpenPrismNode.Web.Models;

public class GetWalletTransactionsResponseModel
{
    public string TransactionId { get; set; }
    public long Fee { get; set; }
    public string? OperationStatusId { get; set; }
    public string? OperationHash { get; set; }
    public string? OperationType { get; set; }
    public string? Status { get; set; }
}