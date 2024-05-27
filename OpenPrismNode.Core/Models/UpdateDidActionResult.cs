namespace OpenPrismNode.Core.Models;

public class UpdateDidActionResult
{
    public UpdateDidActionResult(PrismPublicKey prismPublicKey)
    {
        UpdateDidActionType = UpdateDidActionType.AddKey;
        PrismPublicKey = prismPublicKey;
    }

    public UpdateDidActionResult(string removedKeyId, bool removeService = false)
    {
        if (removeService)
        {
            UpdateDidActionType = UpdateDidActionType.RemoveService;
        }
        else
        {
            UpdateDidActionType = UpdateDidActionType.RemoveKey;
        }

        RemovedKeyId = removedKeyId;
    }

    public UpdateDidActionResult(PrismService prismService)
    {
        UpdateDidActionType = UpdateDidActionType.AddService;
        PrismService = prismService;
    }

    public UpdateDidActionResult(string serviceId, string type, PrismServiceEndpoints serviceEndpoints)
    {
        UpdateDidActionType = UpdateDidActionType.UpdateService;
        PrismService = new PrismService(serviceId, type, serviceEndpoints);
    }
    
    public UpdateDidActionResult(List<string> contexts)
    {
        UpdateDidActionType = UpdateDidActionType.PatchContext;
        Contexts = contexts;
    }

    public PrismService? PrismService { get; }

    public UpdateDidActionType UpdateDidActionType { get; }

    /// <summary>
    /// The public key to be added in this updateOperation
    /// </summary>
    public PrismPublicKey? PrismPublicKey { get; }

    /// <summary>
    /// The keyId of the publicKey which should get removed in this operation
    /// </summary>
    public string? RemovedKeyId { get; }
    
    public List<string>? Contexts { get; }
}