using Inquires.Data;
using Inquires.DTO;
using Inquires.Services;

namespace Inquires.Tests;

public class InquiryServiceTests
{
    [Fact]
    public async Task GetInquiriesAsync_ShouldReturnMappedPagedResult()
    {
        var repository = new FakeInquiryRepository();
        var cache = new FakeCacheService();
        var service = new InquiryService(repository, cache);

        var result = await service.GetInquiriesAsync(new InquiryFilterRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "UpdatedAt",
            SortDescending = true
        }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alpha", result.Items[0].Title);
        Assert.Equal("High", result.Items[0].PriorityName);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectInvalidStatus()
    {
        var repository = new FakeInquiryRepository();
        var cache = new FakeCacheService();
        var service = new InquiryService(repository, cache);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await service.UpdateStatusAsync(1, 999, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPersistAndInvalidateCache_WhenStatusIsValid()
    {
        var repository = new FakeInquiryRepository();
        var cache = new TrackingCacheService();
        var service = new InquiryService(repository, cache);

        var result = await service.UpdateStatusAsync(1, 2, CancellationToken.None);

        Assert.Equal(2, result.StatusId);
        Assert.Equal("InProgress", result.StatusName);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Contains("inquiries:summary", cache.RemovedKeys);
        Assert.Contains("inquiries:list:page1", cache.RemovedKeys);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowNotFound_WhenInquiryDoesNotExist()
    {
        var repository = new FakeInquiryRepository { InquiryExists = false };
        var cache = new FakeCacheService();
        var service = new InquiryService(repository, cache);

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateStatusAsync(999, 1, CancellationToken.None));
    }

    [Fact]
    public async Task GetInquiryByIdAsync_ShouldThrowNotFound_WhenInquiryDoesNotExist()
    {
        var repository = new FakeInquiryRepository { InquiryExists = false };
        var cache = new FakeCacheService();
        var service = new InquiryService(repository, cache);

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetInquiryByIdAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldCacheResultAndNotHitRepositoryOnSecondCall()
    {
        var repository = new CountingFakeInquiryRepository();
        var cache = new TrackingCacheService();
        var service = new InquiryService(repository, cache);

        var result1 = await service.GetSummaryAsync(CancellationToken.None);
        Assert.NotNull(result1);
        Assert.Equal(1, repository.GetSummaryCallCount);
        Assert.Equal(1, cache.SetCallCount);

        var result2 = await service.GetSummaryAsync(CancellationToken.None);
        Assert.Equal(result1, result2);
        Assert.Equal(1, repository.GetSummaryCallCount);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldRecomputeAfterInvalidation()
    {
        var repository = new CountingFakeInquiryRepository();
        var cache = new TrackingCacheService();
        var service = new InquiryService(repository, cache);

        var result1 = await service.GetSummaryAsync(CancellationToken.None);
        Assert.Equal(1, repository.GetSummaryCallCount);

        await cache.RemoveAsync("inquiries:summary", CancellationToken.None);

        var result2 = await service.GetSummaryAsync(CancellationToken.None);
        Assert.Equal(2, repository.GetSummaryCallCount);
    }

    [Fact]
    public async Task GetInquiriesAsync_ShouldBypassFirstPageCache_WhenSortDiffersFromDefault()
    {
        var repository = new CountingFilteredFakeInquiryRepository();
        var cache = new TrackingCacheService();
        var service = new InquiryService(repository, cache);

        // Prime the cache with the default-sorted (CreatedAt desc) first page.
        await service.GetInquiriesAsync(new InquiryFilterRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CreatedAt",
            SortDescending = true
        }, CancellationToken.None);
        Assert.Equal(1, repository.GetFilteredCallCount);

        // A page-1 request asking for a different sort must hit the repository, not the
        // cached default-sort response - this is the bug that made sorting look broken
        // whenever the page-1/no-filters cache had already been primed.
        await service.GetInquiriesAsync(new InquiryFilterRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OrganizationName",
            SortDescending = false
        }, CancellationToken.None);
        Assert.Equal(2, repository.GetFilteredCallCount);
    }

    private class FakeInquiryRepository : IInquiryRepository
    {
        public bool InquiryExists { get; set; } = true;
        public int SaveChangesCallCount { get; private set; }

        public virtual Task<(List<Inquiry> Items, int TotalCount)> GetFilteredAsync(InquiryQueryParameters query, CancellationToken cancellationToken)
        {
            var items = new List<Inquiry>
            {
                new Inquiry
                {
                    InquiryId = 1,
                    Title = "Alpha",
                    OrganizationName = "Contoso",
                    StatusId = 1,
                    PriorityId = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow,
                    Status = new Status { StatusId = 1, Name = "New" },
                    Priority = new Priority { PriorityId = 2, Name = "High" }
                },
                new Inquiry
                {
                    InquiryId = 2,
                    Title = "Beta",
                    OrganizationName = "Fabrikam",
                    StatusId = 2,
                    PriorityId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
                    Status = new Status { StatusId = 2, Name = "InProgress" },
                    Priority = new Priority { PriorityId = 1, Name = "Low" }
                }
            };

            return Task.FromResult((items, items.Count));
        }

        public Task<Inquiry?> GetByIdAsync(int inquiryId, CancellationToken cancellationToken)
        {
            if (!InquiryExists)
                return Task.FromResult<Inquiry?>(null);

            return Task.FromResult<Inquiry?>(new Inquiry
            {
                InquiryId = inquiryId,
                Title = "Alpha",
                OrganizationName = "Contoso",
                StatusId = 1,
                PriorityId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                Status = new Status { StatusId = 1, Name = "New" },
                Priority = new Priority { PriorityId = 2, Name = "High" }
            });
        }

        public Task<bool> StatusExistsAsync(int statusId, CancellationToken cancellationToken)
            => Task.FromResult(statusId == 1 || statusId == 2 || statusId == 3);

        public Task<List<Status>> GetStatusesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<Status>
            {
                new Status { StatusId = 1, Name = "New" },
                new Status { StatusId = 2, Name = "InProgress" },
                new Status { StatusId = 3, Name = "Completed" }
            });

        public Task<List<Priority>> GetPrioritiesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<Priority>
            {
                new Priority { PriorityId = 1, Name = "Low" },
                new Priority { PriorityId = 2, Name = "High" }
            });

        public virtual Task<List<(string StatusName, int Count)>> GetSummaryAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<(string, int)>
            {
                ("New", 5),
                ("InProgress", 3),
                ("Completed", 2)
            });

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, bool useSlidingExpiration = false, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CountingFakeInquiryRepository : FakeInquiryRepository
    {
        public int GetSummaryCallCount { get; private set; }

        public override async Task<List<(string StatusName, int Count)>> GetSummaryAsync(CancellationToken cancellationToken)
        {
            GetSummaryCallCount++;
            return await base.GetSummaryAsync(cancellationToken);
        }
    }

    private sealed class CountingFilteredFakeInquiryRepository : FakeInquiryRepository
    {
        public int GetFilteredCallCount { get; private set; }

        public override async Task<(List<Inquiry> Items, int TotalCount)> GetFilteredAsync(InquiryQueryParameters query, CancellationToken cancellationToken)
        {
            GetFilteredCallCount++;
            return await base.GetFilteredAsync(query, cancellationToken);
        }
    }

    private sealed class TrackingCacheService : ICacheService
    {
        private readonly Dictionary<string, object?> _store = new();
        public int SetCallCount { get; private set; }
        public List<string> RemovedKeys { get; } = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out var value))
                return Task.FromResult((T?)value);
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, bool useSlidingExpiration = false, CancellationToken cancellationToken = default)
        {
            SetCallCount++;
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
