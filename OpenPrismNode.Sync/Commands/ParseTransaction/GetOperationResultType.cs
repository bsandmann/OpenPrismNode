using OpenPrismNode.Sync.Models;

namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using Core.Models;

public static class GetOperationResultType
{
    public static OperationResultType GetFromSignedAtalaOperation(SignedAtalaOperation signedAtalaOperation)
    {
        if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.CreateDid)
        {
            return OperationResultType.CreateDid;
        }
        else if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.UpdateDid)
        {
            return OperationResultType.UpdateDid;
        }
        else if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.ProtocolVersionUpdate)
        {
            return OperationResultType.ProtocolVersionUpdate;
        }
        else if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.DeactivateDid)
        {
            return OperationResultType.DeactivateDid;
        }
        else
        {
            throw new NotImplementedException();
        }
    } 
}