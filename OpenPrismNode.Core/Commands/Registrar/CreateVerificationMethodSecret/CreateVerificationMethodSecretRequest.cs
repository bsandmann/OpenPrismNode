namespace OpenPrismNode.Core.Commands.Registrar.CreateVerificationMethodSecret;

using FluentResults;
using MediatR;
using Models;
using WriteTransaction;

public class CreateVerificationMethodSecretRequest : IRequest<Result>
{
    public CreateVerificationMethodSecretRequest(VerificationMethodSecret verificationMethodSecret, int valueOperationStatusEntityId)
    {
        VerificationMethodSecret = verificationMethodSecret;
        ValueOperationStatusEntityId = valueOperationStatusEntityId;
    }

    public VerificationMethodSecret VerificationMethodSecret { get; }
    public int ValueOperationStatusEntityId { get; }
}