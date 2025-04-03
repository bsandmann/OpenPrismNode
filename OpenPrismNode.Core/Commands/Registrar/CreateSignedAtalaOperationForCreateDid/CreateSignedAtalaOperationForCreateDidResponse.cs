namespace OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForCreateDid;

using Services.Did;

public class CreateSignedAtalaOperationForCreateDidResponse
{
    public CreateSignedAtalaOperationForCreateDidResponse(SignedAtalaOperation signedAtalaOperation, PrismDidTemplate prismDidTemplate)
    {
        SignedAtalaOperation = signedAtalaOperation;
        PrismDidTemplate = prismDidTemplate;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }
    public PrismDidTemplate PrismDidTemplate { get; }
}