namespace OpenPrismNode.Core.Commands.GetStakeAddressesForDay;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GetStakeAddressesForDayHandler : IRequestHandler<GetStakeAddressesForDayRequest, Result<Dictionary<string, int>>>
{
    private readonly DataContext _context;

    public GetStakeAddressesForDayHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<Dictionary<string, int>>> Handle(GetStakeAddressesForDayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var startDate = request.Date.ToDateTime(TimeOnly.MinValue);
            var endDate = request.Date.ToDateTime(TimeOnly.MaxValue);

            var query = _context.BlockEntities
                .Where(b => b.EpochEntity.Ledger == request.Ledger)
                .Where(b => b.TimeUtc >= startDate && b.TimeUtc < endDate)
                .SelectMany(b => b.PrismTransactionEntities)
                .Select(t => new
                {
                    TransactionId = t.TransactionHash,
                    StakeAddresses = t.Utxos
                        .Where(u => u.StakeAddress != null)
                        .Select(u => u.StakeAddress)
                        .Distinct()
                });

            var results = await query.ToListAsync(cancellationToken);

            var stakeAddressCounts = results
                .SelectMany(t => t.StakeAddresses.Select(sa => new { StakeAddress = sa, TransactionId = t.TransactionId }))
                .GroupBy(x => x.StakeAddress)
                .ToDictionary(
                    g => g.Key!,
                    g => g.Select(x => x.TransactionId).Distinct().Count()
                );

            return Result.Ok(stakeAddressCounts);
        }
        catch (Exception ex)
        {
            return Result.Fail($"An error occurred while fetching stake addresses: {ex.Message}");
        }
    }
}