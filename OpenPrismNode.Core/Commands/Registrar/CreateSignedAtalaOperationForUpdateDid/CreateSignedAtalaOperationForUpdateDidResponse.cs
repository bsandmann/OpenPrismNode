namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForUpdateDid;

using OpenPrismNode.Core.Services.Did;

public class CreateSignedAtalaOperationForUpdateDidResponse
{
    public CreateSignedAtalaOperationForUpdateDidResponse(SignedAtalaOperation signedAtalaOperation, PrismDidTemplate prismDidTemplate)
    {
        SignedAtalaOperation = signedAtalaOperation;
        PrismDidTemplate = prismDidTemplate;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }
    public PrismDidTemplate PrismDidTemplate { get; }
}