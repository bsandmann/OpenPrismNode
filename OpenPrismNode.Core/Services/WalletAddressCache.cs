namespace OpenPrismNode.Core.Services;

using Microsoft.Extensions.Caching.Memory;
using OpenPrismNode.Core.Entities;

public class WalletAddressCache : IWalletAddressCache
{
    private readonly IMemoryCache _cache;

    public WalletAddressCache(int maxItems)
    {
        var options = new LruMemoryCacheOptions
        {
            SizeLimit = maxItems,
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        };
        _cache = new LruMemoryCache(options);
    }

    public async Task<WalletAddressEntity?> GetOrAddAsync(string walletAddressString, Func<Task<WalletAddressEntity?>> factory)
    {
        return await _cache.GetOrCreateAsync(walletAddressString, async entry =>
        {
            entry.SetSize(1);
            var result = await factory();
            return result;
        });
    }

    public Task SetAsync(string walletAddressString, WalletAddressEntity walletAddress)
    {
        _cache.Set(walletAddressString, walletAddress, new MemoryCacheEntryOptions
        {
            Size = 1,
        });
        return Task.CompletedTask;
    }
}