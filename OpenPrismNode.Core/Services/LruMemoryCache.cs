using Microsoft.Extensions.Caching.Memory;

namespace OpenPrismNode.Core.Services;

public class LruMemoryCache : MemoryCache
{
    public LruMemoryCache(LruMemoryCacheOptions options) : base(options)
    {
        if (options.SizeLimit <= 0)
        {
            throw new ArgumentException("SizeLimit must be positive.", nameof(options));
        }
    }
}