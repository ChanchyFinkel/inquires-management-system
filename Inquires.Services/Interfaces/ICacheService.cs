namespace Inquires.Services;

public interface ICacheService
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or default if missing or expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Caches <paramref name="value"/> under <paramref name="key"/>. A null <paramref name="ttl"/> never expires; <paramref name="useSlidingExpiration"/> resets the TTL on each access instead of counting down from insertion.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, bool useSlidingExpiration = false, CancellationToken cancellationToken = default);

    /// <summary>Removes the entry for the exact <paramref name="key"/>, if present.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
