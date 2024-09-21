namespace OpenPrismNode.Core.Commands.Withdrawal;

using FluentResults;
using MediatR;

public class WithdrawalRequest : IRequest<Result>
{
    public string WalletId { get; set; }

    public string WithdrawalAddress { get; set; }
}