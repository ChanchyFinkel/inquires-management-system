using System.ComponentModel.DataAnnotations;

namespace Inquiries.DTO;

public class InquiryFilterRequest
{
    [MaxLength(200)]
    public string? SearchTerm { get; set; }

    [Range(1, int.MaxValue)]
    public int? StatusId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PriorityId { get; set; }

    [MaxLength(200)]
    public string? OrganizationName { get; set; }

    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
