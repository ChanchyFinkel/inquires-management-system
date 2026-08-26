using Microsoft.Extensions.Caching.Memory;

namespace Inquiries.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

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

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
