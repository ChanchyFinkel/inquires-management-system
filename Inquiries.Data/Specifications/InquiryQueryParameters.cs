namespace Inquiries.Data;

public class InquiryQueryParameters
{
    public string? SearchTerm { get; set; }
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public string? OrganizationName { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
