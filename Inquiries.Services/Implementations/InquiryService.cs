using Inquiries.Data;
using Inquiries.DTO;

namespace Inquiries.Services;

public class InquiryService : IInquiryService
{
    private const string StatusesCacheKey = "inquiries:statuses";
    private const string PrioritiesCacheKey = "inquiries:priorities";
    private const string SummaryCacheKey = "inquiries:summary";
    private const string FirstPageCacheKey = "inquiries:list:page1";

    private static readonly TimeSpan LookupTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SummaryTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FirstPageTtl = TimeSpan.FromSeconds(60);

    private readonly IInquiryRepository _repository;
    private readonly ICacheService _cache;

    public InquiryService(IInquiryRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PagedResult<InquiryResponse>> GetInquiriesAsync(InquiryFilterRequest query, CancellationToken cancellationToken)
    {
        var normalizedQuery = new InquiryQueryParameters
        {
            SearchTerm = query.SearchTerm,
            StatusId = query.StatusId,
            PriorityId = query.PriorityId,
            OrganizationName = query.OrganizationName,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        var isFirstPage = query.PageNumber == 1
            && query.PageSize > 0
            && query.SortBy == "CreatedAt"
            && query.SortDescending
            && string.IsNullOrWhiteSpace(query.SearchTerm)
            && !query.StatusId.HasValue
            && !query.PriorityId.HasValue
            && string.IsNullOrWhiteSpace(query.OrganizationName);

        if (isFirstPage)
        {
            var cached = await _cache.GetAsync<PagedResult<InquiryResponse>>(FirstPageCacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var (items, totalCount) = await _repository.GetFilteredAsync(normalizedQuery, cancellationToken);
        var response = items
            .Select(inquiry => inquiry.ToDto())
            .ToPagedResult(totalCount, query.PageNumber, query.PageSize);

        if (isFirstPage)
        {
            await _cache.SetAsync(FirstPageCacheKey, response, FirstPageTtl, cancellationToken: cancellationToken);
        }

        return response;
    }

    public async Task<InquiryResponse> GetInquiryByIdAsync(int inquiryId, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(inquiryId, cancellationToken)
            ?? throw new NotFoundException($"Inquiry {inquiryId} was not found.");

        return inquiry.ToDto();
    }

    public async Task<InquiryResponse> UpdateStatusAsync(int inquiryId, int statusId, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(inquiryId, cancellationToken)
            ?? throw new NotFoundException($"Inquiry {inquiryId} was not found.");

        if (!await _repository.StatusExistsAsync(statusId, cancellationToken))
        {
            throw new ValidationException("The specified status does not exist.");
        }

        inquiry.StatusId = statusId;
        inquiry.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(SummaryCacheKey, cancellationToken);
        await _cache.RemoveAsync(FirstPageCacheKey, cancellationToken);

        var response = inquiry.ToDto();
        var updatedStatus = (await GetStatusesAsync(cancellationToken))
            .FirstOrDefault(status => status.StatusId == statusId);
        if (updatedStatus is not null)
            response.StatusName = updatedStatus.Name;

        return response;
    }

    public async Task<IReadOnlyList<StatusResponse>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<List<StatusResponse>>(StatusesCacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var statuses = await _repository.GetStatusesAsync(cancellationToken);
        var statusDtos = statuses.Select(status => status.ToDto()).ToList();
        await _cache.SetAsync(StatusesCacheKey, statusDtos, LookupTtl, useSlidingExpiration: true, cancellationToken);
        return statusDtos;
    }

    public async Task<IReadOnlyList<PriorityResponse>> GetPrioritiesAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<List<PriorityResponse>>(PrioritiesCacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var priorities = await _repository.GetPrioritiesAsync(cancellationToken);
        var priorityDtos = priorities.Select(priority => priority.ToDto()).ToList();
        await _cache.SetAsync(PrioritiesCacheKey, priorityDtos, LookupTtl, useSlidingExpiration: true, cancellationToken);
        return priorityDtos;
    }

    public async Task<IReadOnlyList<InquirySummary>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<List<InquirySummary>>(SummaryCacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var summary = await _repository.GetSummaryAsync(cancellationToken);
        var summaryDtos = summary
            .Select(s => new InquirySummary { StatusName = s.StatusName, Count = s.Count })
            .ToList();

        await _cache.SetAsync(SummaryCacheKey, summaryDtos, SummaryTtl, cancellationToken: cancellationToken);
        return summaryDtos;
    }
}
