namespace OpenPrismNode.Core.Commands.WriteTransaction;

using FluentResults;
using MediatR;

public class WriteTransactionRequest : IRequest<Result<WriteTransactionResponse>>
{
    public string WalletId { get; set; }

    public SignedAtalaOperation SignedAtalaOperation { get; set; }
}