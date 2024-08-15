using Microsoft.Extensions.Caching.Memory;

namespace OpenPrismNode.Core.Services;

/// <summary>
/// Least Recently Used Memory Cache Options
/// </summary>
public class LruMemoryCacheOptions : MemoryCacheOptions
{
    public int SizeLimit { get; set; }
}