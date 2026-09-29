using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Payment_ServiceAPI.Authentication;
using Payment_ServiceAPI.DTOs.Premiums;
using Payment_ServiceAPI.Services.Interfaces;

namespace Payment_ServiceAPI.Controllers;

[ApiController]
[Route("api/premium/schedules")]
[Authorize(Policy = PaymentServicePolicies.PaymentRead)]
public sealed class PremiumSchedulesController : ControllerBase
{
    private readonly IPremiumScheduleService _premiumScheduleService;

    public PremiumSchedulesController(IPremiumScheduleService premiumScheduleService)
    {
        _premiumScheduleService = premiumScheduleService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PremiumScheduleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PremiumScheduleResponse>>> Get(
        [FromQuery] Guid policyId,
        CancellationToken cancellationToken)
    {
        return Ok(await _premiumScheduleService.GetAsync(policyId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    [ProducesResponseType<IReadOnlyCollection<PremiumScheduleResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<PremiumScheduleResponse>>> Create(
        [FromBody] CreatePremiumScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var schedules = await _premiumScheduleService.CreateAsync(request, ResolveActor(), cancellationToken);
            return CreatedAtAction(nameof(Get), new { policyId = request.PolicyId }, schedules);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(CreateProblem(exception.Message, StatusCodes.Status409Conflict));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblem(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    private string ResolveActor() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.Identity?.Name
        ?? "unknown";

    private static ProblemDetails CreateProblem(string detail, int status) => new()
    {
        Title = "Premium schedule request failed.",
        Detail = detail,
        Status = status
    };
}