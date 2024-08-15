namespace OpenPrismNode.Core.Services;

using Microsoft.Extensions.Caching.Memory;
using OpenPrismNode.Core.Entities;

public class StakeAddressCache : IStakeAddressCache
{
    private readonly IMemoryCache _cache;

    public StakeAddressCache(int maxItems)
    {
        var options = new LruMemoryCacheOptions
        {
            SizeLimit = maxItems,
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        };
        _cache = new LruMemoryCache(options);
    }

    public async Task<StakeAddressEntity?> GetOrAddAsync(string stakeAddressString, Func<Task<StakeAddressEntity?>> factory)
    {
        return await _cache.GetOrCreateAsync(stakeAddressString, async entry =>
        {
            entry.SetSize(1);
            var result = await factory();
            return result;
        });
    }

    public Task SetAsync(string stakeAddressString, StakeAddressEntity stakeAddress)
    {
        _cache.Set(stakeAddressString, stakeAddress, new MemoryCacheEntryOptions
        {
            Size = 1,
        });
        return Task.CompletedTask;
    }
}