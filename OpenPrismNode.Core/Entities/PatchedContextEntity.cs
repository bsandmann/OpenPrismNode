namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class PatchedContextEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    public int PatchedContextEntityId { get; set; }

    /// <summary>
    /// List of patched Contexts. List of strings. Cannot be null, but an empty list is allowed.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ContextListJson { get; set; }

    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    [Column(TypeName = "smallint")]
    public int? UpdateOperationOrder { get; set; }

    /// <summary>
    /// References to the linked operation which added this service
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] UpdateDidEntityOperationHash { get; set; }
    
    public UpdateDidEntity UpdateDidEntity { get; set; }
    
    [NotMapped]
    public List<string> ContextList
    {
        get => JsonSerializer.Deserialize<List<string>>(ContextListJson)!;
        set => ContextListJson = JsonSerializer.Serialize(value);
    }
}