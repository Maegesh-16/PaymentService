using Microsoft.AspNetCore.Mvc;

namespace Payment_ServiceAPI.Controllers;

[ApiController]
public abstract class PaymentControllerBase : ControllerBase
{
    protected ActionResult HandleInvalidOperation(InvalidOperationException exception)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Payment service validation failed.",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
