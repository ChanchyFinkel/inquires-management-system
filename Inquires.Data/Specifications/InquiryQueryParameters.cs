namespace Inquires.Data;

// Named differently from Inquires.DTO.InquiryFilterRequest on purpose: this is the shape the
// repository needs to build a query, while InquiryFilterRequest is the shape the API accepts
// from an untrusted client. Giving them the same name (as this class briefly had) forces every
// caller that needs both types in scope to disambiguate with a using-alias - giving them
// distinct names removes that friction entirely and makes it obvious at a glance which one
// you're looking at.
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
