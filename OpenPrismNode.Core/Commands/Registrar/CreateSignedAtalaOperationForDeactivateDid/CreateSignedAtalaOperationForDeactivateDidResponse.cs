namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForDeactivateDid;

public class CreateSignedAtalaOperationForDeactivateDidResponse
{
    public CreateSignedAtalaOperationForDeactivateDidResponse(SignedAtalaOperation signedAtalaOperation)
    {
        SignedAtalaOperation = signedAtalaOperation;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }
}