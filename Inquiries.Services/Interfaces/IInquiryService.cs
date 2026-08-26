using Inquiries.DTO;

namespace Inquiries.Services;

public interface IInquiryService
{
    /// <summary>Returns a filtered, sorted, paged list of inquiries.</summary>
    Task<PagedResult<InquiryResponse>> GetInquiriesAsync(InquiryFilterRequest query, CancellationToken cancellationToken);

    /// <summary>Returns a single inquiry by id, or throws <see cref="NotFoundException"/> if it doesn't exist.</summary>
    Task<InquiryResponse> GetInquiryByIdAsync(int inquiryId, CancellationToken cancellationToken);

    /// <summary>Updates an inquiry's status, throwing <see cref="NotFoundException"/> if the inquiry doesn't exist or <see cref="ValidationException"/> if the status id is invalid.</summary>
    Task<InquiryResponse> UpdateStatusAsync(int inquiryId, int statusId, CancellationToken cancellationToken);

    /// <summary>Returns all available inquiry statuses.</summary>
    Task<IReadOnlyList<StatusResponse>> GetStatusesAsync(CancellationToken cancellationToken);

    /// <summary>Returns all available inquiry priorities.</summary>
    Task<IReadOnlyList<PriorityResponse>> GetPrioritiesAsync(CancellationToken cancellationToken);

    /// <summary>Returns inquiry counts grouped by status.</summary>
    Task<IReadOnlyList<InquirySummary>> GetSummaryAsync(CancellationToken cancellationToken);
}
