namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;


/// <summary>
/// Result of of Select operation againt the db
/// </summary>
public class PatchedContextResult
{
    /// <summary>
    /// List of patched Contexts. List of strings. Cannot be null, but an empty list is allowed.
    /// </summary>
    public List<string> ContextList { get; set; } 

    /// <summary>
    /// Optional property for the updateOperation
    /// </summary>
    public int? UpdateOperationOrder { get; set; }
}