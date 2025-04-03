namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class VerificationMethodSecretEntity
{
    public int VerificationMethodSecretEntityId { get; set; }

    [Required] [MaxLength(100)] public string PrismKeyUsage { get; set; }
    [Required] [MaxLength(255)] public string KeyId { get; set; }
    [Required] [MaxLength(100)] public string Curve { get; set; }
    public byte[] Bytes { get; set; }
    public bool IsRemoveOperation { get; set; }
    
    [MaxLength(1000)] public string? Mnemonic { get; set; }
    public int OperationStatusEntityId { get; set; }
    public OperationStatusEntity OperationStatusEntity { get; set; }
}