namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using Core.Models;

public static class UpdateStackEvaluation
{
    public static bool UpdateActionStackContainsKeyId(List<UpdateDidActionResult> updateActionResults, string keyId)
    {
        return updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.AddKey && p.PrismPublicKey is not null && p.PrismPublicKey.KeyId.Equals(keyId)) ||
               updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.RemoveKey && p.RemovedKeyId == keyId);
    }

    public static bool UpdateActionStackContainsServiceId(List<UpdateDidActionResult> updateActionResults, string serviceId)
    {
        return updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.AddService && p.PrismService is not null && p.PrismService.ServiceId.Equals(serviceId)) ||
               updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.UpdateService && p.PrismService is not null && p.PrismService.ServiceId.Equals(serviceId)) ||
               updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.RemoveService && p.RemovedKeyId == serviceId);
    }

    public static bool UpdateActionStackLastKeyActionWasAddKey(List<UpdateDidActionResult> updateActionResults, string keyId)
    {
        var lastActionWasAdd = false;
        foreach (var updateActionResult in updateActionResults)
        {
            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.AddKey && updateActionResult.PrismPublicKey is not null && updateActionResult.PrismPublicKey.KeyId.Equals(keyId))
            {
                lastActionWasAdd = true;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.RemoveKey && updateActionResult.RemovedKeyId == keyId)
            {
                lastActionWasAdd = false;
            }
        }

        return lastActionWasAdd;
    }

    public static bool UpdateActionStackLastKeyActionWasRemoveKey(List<UpdateDidActionResult> updateActionResults, string keyId)
    {
        var lastActionWasRemove = false;
        foreach (var updateActionResult in updateActionResults)
        {
            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.RemoveKey && updateActionResult.RemovedKeyId == keyId)
            {
                lastActionWasRemove = true;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.AddKey && updateActionResult.PrismPublicKey is not null && updateActionResult.PrismPublicKey.KeyId.Equals(keyId))
            {
                lastActionWasRemove = false;
            }
        }

        return lastActionWasRemove;
    }

    public static bool UpdateActionStackLastServiceActionWasAddService(List<UpdateDidActionResult> updateActionResults, string serviceId)
    {
        var lastActionWasAdd = false;
        foreach (var updateActionResult in updateActionResults)
        {
            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.AddService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasAdd = true;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.RemoveService && updateActionResult.RemovedKeyId == serviceId)
            {
                lastActionWasAdd = false;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.UpdateService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasAdd = false;
            }
        }

        return lastActionWasAdd;
    }

    public static bool UpdateActionStackLastServiceActionWasRemoveService(List<UpdateDidActionResult> updateActionResults, string serviceId)
    {
        var lastActionWasRemove = false;
        foreach (var updateActionResult in updateActionResults)
        {
            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.RemoveService && updateActionResult.RemovedKeyId == serviceId)
            {
                lastActionWasRemove = true;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.AddService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasRemove = false;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.UpdateService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasRemove = false;
            }
        }

        return lastActionWasRemove;
    }

    public static bool UpdateActionStackLastServiceActionWasUpdateService(List<UpdateDidActionResult> updateActionResults, string serviceId)
    {
        var lastActionWasUpdate = false;
        foreach (var updateActionResult in updateActionResults)
        {
            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.UpdateService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasUpdate = true;
            }

            if (updateActionResult.UpdateDidActionType == UpdateDidActionType.RemoveService && updateActionResult.RemovedKeyId == serviceId)
            {
                lastActionWasUpdate = false;
            }
            else if (updateActionResult.UpdateDidActionType == UpdateDidActionType.AddService && updateActionResult.PrismService is not null && updateActionResult.PrismService.ServiceId.Equals(serviceId))
            {
                lastActionWasUpdate = false;
            }
        }

        return lastActionWasUpdate;
    }
}