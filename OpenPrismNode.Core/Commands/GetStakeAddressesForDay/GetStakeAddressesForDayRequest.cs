namespace OpenPrismNode.Core.Commands.GetStakeAddressesForDay;

using FluentResults;
using MediatR;
using Models;

public class GetStakeAddressesForDayRequest : IRequest<Result<Dictionary<string, int>>>
{
    public GetStakeAddressesForDayRequest(LedgerType ledger, DateOnly date)
    {
        Ledger = ledger;
        Date = date;
    }

    public LedgerType Ledger { get; }
    public DateOnly Date { get; }
}