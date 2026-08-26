namespace Inquiries.Data;

public interface IInquiryRepository
{
    Task<(List<Inquiry> Items, int TotalCount)> GetFilteredAsync(
        InquiryQueryParameters query,
        CancellationToken cancellationToken);

    Task<Inquiry?> GetByIdAsync(int inquiryId, CancellationToken cancellationToken);
    Task<bool> StatusExistsAsync(int statusId, CancellationToken cancellationToken);
    Task<List<Status>> GetStatusesAsync(CancellationToken cancellationToken);
    Task<List<Priority>> GetPrioritiesAsync(CancellationToken cancellationToken);
    Task<List<(string StatusName, int Count)>> GetSummaryAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
