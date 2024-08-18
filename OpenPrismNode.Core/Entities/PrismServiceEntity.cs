namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class PrismServiceEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    public int PrismServiceEntityId { get; set; }

    /// <summary>
    /// Name of the service 
    /// </summary>
    [MaxLength(50)]
    public string ServiceId { get; set; }

    /// <summary>
    /// Service type eg. LinkedDomains
    /// Note, can be null, if the service gets removed
    /// </summary>
    [MaxLength(100)]
    public string? Type { get; set; }

    public string? UriString { get; set; }

    [Column(TypeName = "jsonb")] public string? JsonData { get; set; }

    [Column(TypeName = "jsonb")] public string? ListOfUrisJson { get; set; }


    /// <summary>
    /// When a remove service-action is performed this flag gets set
    /// </summary>
    public bool Removed { get; set; }
    
    /// <summary>
    /// When an update occures this flag is set
    /// </summary>
    public bool Updated { get; set; }

    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    [Column(TypeName = "smallint")]
    public int? UpdateOperationOrder { get; set; }

    /// <summary>
    /// References to the linked operation which added this service
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[]? UpdateDidEntityOperationHash { get; set; }

    /// <summary>
    /// References to the linked operation which created this service
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[]? CreateDidEntityOperationHash { get; set; }
    
    [NotMapped]
    public Uri? Uri
    {
        get => UriString != null ? new Uri(UriString) : null;
        set => UriString = value?.ToString();
    }

    [NotMapped]
    public List<Uri>? ListOfUris
    {
        get => ListOfUrisJson != null 
            ? JsonSerializer.Deserialize<List<string>>(ListOfUrisJson)?.Select(s => new Uri(s)).ToList() 
            : null;
        set => ListOfUrisJson = value != null 
            ? JsonSerializer.Serialize(value.Select(u => u.ToString()).ToList()) 
            : null;
    }
}