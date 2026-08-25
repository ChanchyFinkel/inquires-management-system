using System.ComponentModel.DataAnnotations;

namespace Inquires.DTO;

public class UpdateInquiryStatusRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StatusId must be a positive value.")]
    public int StatusId { get; set; }
}
