using Inquires.DTO;
using Inquires.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inquires.Api.Controllers;

[ApiController]
[Route("api/inquiries")]
public class InquiriesController : ControllerBase
{
    private readonly IInquiryService _inquiryService;

    public InquiriesController(IInquiryService inquiryService)
    {
        _inquiryService = inquiryService;
    }

    /// <summary>Returns a filtered, sorted, paged list of inquiries.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<InquiryResponse>>> GetInquiries(
        [FromQuery] InquiryFilterRequest query, CancellationToken cancellationToken)
    {
        var result = await _inquiryService.GetInquiriesAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single inquiry by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InquiryResponse>> GetInquiryById(int id, CancellationToken cancellationToken)
    {
        var result = await _inquiryService.GetInquiryByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Updates an inquiry's status.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<InquiryResponse>> UpdateInquiryStatus(
        int id, [FromBody] UpdateInquiryStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _inquiryService.UpdateStatusAsync(id, request.StatusId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns inquiry counts grouped by status.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyList<InquirySummary>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _inquiryService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns all available inquiry statuses, for filter dropdowns.</summary>
    [HttpGet("statuses")]
    public async Task<ActionResult<IReadOnlyList<StatusResponse>>> GetStatuses(CancellationToken cancellationToken)
    {
        var result = await _inquiryService.GetStatusesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns all available inquiry priorities, for filter dropdowns.</summary>
    [HttpGet("priorities")]
    public async Task<ActionResult<IReadOnlyList<PriorityResponse>>> GetPriorities(CancellationToken cancellationToken)
    {
        var result = await _inquiryService.GetPrioritiesAsync(cancellationToken);
        return Ok(result);
    }
}
