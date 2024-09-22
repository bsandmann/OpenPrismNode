namespace OpenPrismNode.Core.Commands.WriteTransaction;

using FluentResults;
using MediatR;

public class WriteTransactionRequest : IRequest<Result<WriteTransactionResponse>>
{
    public WriteTransactionRequest(SignedAtalaOperation signedAtalaOperation, string walletId)
    {
        SignedAtalaOperation = signedAtalaOperation;
        WalletId = walletId;
    }

    public string WalletId { get; }

    public SignedAtalaOperation SignedAtalaOperation { get; }
}