using System.ComponentModel.DataAnnotations;

namespace Inquiries.DTO;

public class UpdateInquiryStatusRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StatusId must be a positive value.")]
    public int StatusId { get; set; }
}
