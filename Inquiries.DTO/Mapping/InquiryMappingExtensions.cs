using Inquiries.Data;

namespace Inquiries.DTO;

public static class InquiryMappingExtensions
{
    public static InquiryResponse ToDto(this Inquiry inquiry)
    {
        return new InquiryResponse
        {
            InquiryId = inquiry.InquiryId,
            Title = inquiry.Title,
            OrganizationName = inquiry.OrganizationName,
            StatusId = inquiry.StatusId,
            StatusName = inquiry.Status?.Name ?? string.Empty,
            PriorityId = inquiry.PriorityId,
            PriorityName = inquiry.Priority?.Name ?? string.Empty,
            CreatedAt = inquiry.CreatedAt,
            UpdatedAt = inquiry.UpdatedAt
        };
    }

    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public static StatusResponse ToDto(this Status status)
    {
        return new StatusResponse
        {
            StatusId = status.StatusId,
            Name = status.Name
        };
    }

    public static PriorityResponse ToDto(this Priority priority)
    {
        return new PriorityResponse
        {
            PriorityId = priority.PriorityId,
            Name = priority.Name
        };
    }
}
