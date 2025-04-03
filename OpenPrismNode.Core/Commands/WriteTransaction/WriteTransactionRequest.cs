namespace OpenPrismNode.Core.Commands.WriteTransaction;

using Entities;
using FluentResults;
using MediatR;
using Models;

public class WriteTransactionRequest : IRequest<Result<WriteTransactionResponse>>
{
    public WriteTransactionRequest(SignedAtalaOperation signedAtalaOperation, string walletId, List<VerificationMethodSecret>? verificationMethodSecrets = null)
    {
        SignedAtalaOperation = signedAtalaOperation;
        WalletId = walletId;
        VerificationMethodSecrets = verificationMethodSecrets;
    }

    public string WalletId { get; }
    public SignedAtalaOperation SignedAtalaOperation { get; }

    public List<VerificationMethodSecret>? VerificationMethodSecrets { get; }
}