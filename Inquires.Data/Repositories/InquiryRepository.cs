using Microsoft.EntityFrameworkCore;

namespace Inquires.Data;

public class InquiryRepository : IInquiryRepository
{
    private readonly InquiresDbContext _context;

    public InquiryRepository(InquiresDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Inquiry> Items, int TotalCount)> GetFilteredAsync(
        InquiryQueryParameters query,
        CancellationToken cancellationToken)
    {
        IQueryable<Inquiry> inquiries = _context.Inquiries
            .AsNoTracking()
            .Include(inquiry => inquiry.Status)
            .Include(inquiry => inquiry.Priority);

        if (query.StatusId.HasValue)
            inquiries = inquiries.Where(inquiry => inquiry.StatusId == query.StatusId.Value);

        if (query.PriorityId.HasValue)
            inquiries = inquiries.Where(inquiry => inquiry.PriorityId == query.PriorityId.Value);

        if (!string.IsNullOrWhiteSpace(query.OrganizationName))
            inquiries = inquiries.Where(inquiry => inquiry.OrganizationName.Contains(query.OrganizationName));

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            inquiries = inquiries.Where(inquiry =>
                inquiry.Title.Contains(query.SearchTerm)
                || inquiry.OrganizationName.Contains(query.SearchTerm));
        }

        var totalCount = await inquiries.CountAsync(cancellationToken);
        inquiries = ApplySort(inquiries, query.SortBy, query.SortDescending);

        var items = await inquiries
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Inquiry?> GetByIdAsync(int inquiryId, CancellationToken cancellationToken)
    {
        return _context.Inquiries
            .Include(inquiry => inquiry.Status)
            .Include(inquiry => inquiry.Priority)
            .SingleOrDefaultAsync(inquiry => inquiry.InquiryId == inquiryId, cancellationToken);
    }

    public Task<bool> StatusExistsAsync(int statusId, CancellationToken cancellationToken)
    {
        return _context.Statuses
            .AsNoTracking()
            .AnyAsync(status => status.StatusId == statusId, cancellationToken);
    }

    public Task<List<Status>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        return _context.Statuses
            .AsNoTracking()
            .OrderBy(status => status.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Priority>> GetPrioritiesAsync(CancellationToken cancellationToken)
    {
        return _context.Priorities
            .AsNoTracking()
            .OrderBy(priority => priority.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Inquiry> ApplySort(
        IQueryable<Inquiry> inquiries,
        string? sortBy,
        bool sortDescending)
    {
        IOrderedQueryable<Inquiry> sorted = (sortBy, sortDescending) switch
        {
            ("Title", false) => inquiries.OrderBy(inquiry => inquiry.Title),
            ("Title", true) => inquiries.OrderByDescending(inquiry => inquiry.Title),
            ("OrganizationName", false) => inquiries.OrderBy(inquiry => inquiry.OrganizationName),
            ("OrganizationName", true) => inquiries.OrderByDescending(inquiry => inquiry.OrganizationName),
            ("Status", false) => inquiries.OrderBy(inquiry => inquiry.Status.Name),
            ("Status", true) => inquiries.OrderByDescending(inquiry => inquiry.Status.Name),
            ("Priority", false) => inquiries.OrderBy(inquiry => inquiry.Priority.Name),
            ("Priority", true) => inquiries.OrderByDescending(inquiry => inquiry.Priority.Name),
            ("UpdatedAt", false) => inquiries.OrderBy(inquiry => inquiry.UpdatedAt),
            ("UpdatedAt", true) => inquiries.OrderByDescending(inquiry => inquiry.UpdatedAt),
            (_, false) => inquiries.OrderBy(inquiry => inquiry.CreatedAt),
            _ => inquiries.OrderByDescending(inquiry => inquiry.CreatedAt)
        };

        return sorted.ThenBy(inquiry => inquiry.InquiryId);
    }

    public async Task<List<(string StatusName, int Count)>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _context.Inquiries
            .AsNoTracking()
            .GroupBy(inquiry => inquiry.Status.Name)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(s => s.Status)
            .ToListAsync(cancellationToken);

        return summary.Select(s => (s.Status, s.Count)).ToList();
    }
}
