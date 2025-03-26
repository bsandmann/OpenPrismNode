namespace OpenPrismNode.Core.Commands.GetStakeAddressesForDay;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;

public class GetStakeAddressesForDayHandler : IRequestHandler<GetStakeAddressesForDayRequest, Result<Dictionary<string, int>>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAppCache _cache;

    public GetStakeAddressesForDayHandler(IServiceScopeFactory serviceScopeFactory, IAppCache cache)
    {
         _serviceScopeFactory = serviceScopeFactory;
        _cache = cache;
    }

    public async Task<Result<Dictionary<string, int>>> Handle(GetStakeAddressesForDayRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.StakeAddressesByDay_}_{request.Ledger}_{request.Date:yyyy-MM-dd}";

        var isCached = _cache.TryGetValue(cacheKey, out Dictionary<string, int> cachedResult);
        if (isCached)
        {
            return Result.Ok(cachedResult);
        }

        var result = await FetchStakeAddresses(request, cancellationToken);

        if (result.IsSuccess)
        {
            var cacheOptions = GetCacheOptions(request.Date);
            _cache.Add(cacheKey, result.Value, cacheOptions);
        }

        return result;
    }

    private async Task<Result<Dictionary<string, int>>> FetchStakeAddresses(GetStakeAddressesForDayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var startDate = request.Date.ToDateTime(TimeOnly.MinValue);
            var endDate = request.Date.ToDateTime(TimeOnly.MaxValue);

            var query = context.BlockEntities
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

    private TimeSpan GetCacheOptions(DateOnly date)
    {
        if (date == DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return TimeSpan.FromMinutes(15);
        }
        else
        {
            return TimeSpan.FromDays(365);
        }
    }
}