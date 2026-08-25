using System.ComponentModel.DataAnnotations.Schema;

namespace Inquires.Data;

public class Inquiry
{
    public int InquiryId { get; set; }

    public string Title { get; set; } = null!;

    public string OrganizationName { get; set; } = null!;

    public int StatusId { get; set; }

    public int PriorityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

	[ForeignKey("StatusId")]
    public Status Status { get; set; } = null!;

	[ForeignKey("PriorityId")]
    public Priority Priority { get; set; } = null!;
}
