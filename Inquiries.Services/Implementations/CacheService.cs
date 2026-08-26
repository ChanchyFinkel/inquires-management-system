using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Inquiries.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out T? value) ? value : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, bool useSlidingExpiration = false, CancellationToken cancellationToken = default)
    {
        _trackedKeys.TryAdd(key, 0);

        if (!ttl.HasValue)
        {
            _memoryCache.Set(key, value);
            return Task.CompletedTask;
        }

        var options = new MemoryCacheEntryOptions();
        if (useSlidingExpiration)
            options.SlidingExpiration = ttl.Value;
        else
            options.AbsoluteExpirationRelativeToNow = ttl.Value;

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) => _trackedKeys.TryRemove((string)evictedKey, out _));

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _trackedKeys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _memoryCache.Remove(key);
                _trackedKeys.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
