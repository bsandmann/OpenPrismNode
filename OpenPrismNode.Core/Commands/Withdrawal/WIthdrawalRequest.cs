namespace OpenPrismNode.Core.Commands.Withdrawal;

using FluentResults;
using MediatR;

public class WithdrawalRequest : IRequest<Result>
{
    public WithdrawalRequest(string walletId, string withdrawalAddress)
    {
        WalletId = walletId;
        WithdrawalAddress = withdrawalAddress;
    }

    public string WalletId { get; }

    public string WithdrawalAddress { get; }
}