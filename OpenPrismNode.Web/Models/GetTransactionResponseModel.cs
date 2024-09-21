namespace OpenPrismNode.Web.Models;

using Core.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

public class GetTransactionResponseModel
{
    /// <summary>
    /// The operationId used for the query (Hex encoded hash of the signedAtalaOperation)
    /// </summary>
    public string OperationId { get; set; }

    /// <summary>
    /// Hash of the operation inside the signedAtalaOperation as Hex
    /// </summary>
    public string OperationHash { get; set; }

    public DateTime CreatedUtc { get; set; }
    
    public DateTime? LastUpdatedUtc { get; set; }
    
    public string Status { get; set; }
}