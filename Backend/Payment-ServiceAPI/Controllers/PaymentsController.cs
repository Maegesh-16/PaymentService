using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Payment_ServiceAPI.Authentication;
using Payment_ServiceAPI.DTOs.Payments;
using Payment_ServiceAPI.Services.Interfaces;

namespace Payment_ServiceAPI.Controllers;

[Route("api/payments")]
[Authorize(Policy = PaymentServicePolicies.PaymentRead)]
public class PaymentsController : PaymentControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> GetPayments([FromQuery] Guid? policyId, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetPaymentsAsync(policyId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] CreatePaymentDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required.",
                Detail = "Provide the Idempotency-Key header for create operations.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _paymentService.CreatePaymentAsync(request, idempotencyKey, ResolveActor(), HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(GetPayments), new { policyId = result.PolicyId }, result);
        }
        catch (InvalidOperationException exception)
        {
            return HandleInvalidOperation(exception);
        }
    }

    [HttpPost("checkout")]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    public async Task<ActionResult<CheckoutPaymentResponse>> Checkout([FromBody] CheckoutPaymentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required.",
                Detail = "Provide the Idempotency-Key header for checkout operations.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _paymentService.CheckoutAsync(request, idempotencyKey, ResolveActor(), HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(GetPayments), new { policyId = result.Payment.PolicyId }, result);
        }
        catch (InvalidOperationException exception)
        {
            return HandleInvalidOperation(exception);
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyCollection<PaymentTransactionDto>>> GetTransactions([FromQuery] Guid? paymentId, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetTransactionsAsync(paymentId, cancellationToken));
    }

    [HttpPost("transactions")]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    public async Task<ActionResult<PaymentTransactionDto>> CreateTransaction([FromBody] CreatePaymentTransactionDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required.",
                Detail = "Provide the Idempotency-Key header for create operations.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _paymentService.CreateTransactionAsync(request, idempotencyKey, ResolveActor(), HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(GetTransactions), new { paymentId = result.PaymentId }, result);
        }
        catch (InvalidOperationException exception)
        {
            return HandleInvalidOperation(exception);
        }
    }

    [HttpGet("refunds")]
    public async Task<ActionResult<IReadOnlyCollection<RefundDto>>> GetRefunds([FromQuery] Guid? paymentId, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetRefundsAsync(paymentId, cancellationToken));
    }

    [HttpPost("refunds")]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    public async Task<ActionResult<RefundDto>> CreateRefund([FromBody] CreateRefundDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required.",
                Detail = "Provide the Idempotency-Key header for create operations.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _paymentService.CreateRefundAsync(request, idempotencyKey, ResolveActor(), HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(GetRefunds), new { paymentId = result.PaymentId }, result);
        }
        catch (InvalidOperationException exception)
        {
            return HandleInvalidOperation(exception);
        }
    }

    [HttpGet("receipts")]
    public async Task<ActionResult<IReadOnlyCollection<ReceiptDto>>> GetReceipts([FromQuery] Guid? paymentId, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetReceiptsAsync(paymentId, cancellationToken));
    }

    [HttpPost("receipts")]
    [Authorize(Policy = PaymentServicePolicies.PaymentWrite)]
    public async Task<ActionResult<ReceiptDto>> CreateReceipt([FromBody] CreateReceiptDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required.",
                Detail = "Provide the Idempotency-Key header for create operations.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _paymentService.CreateReceiptAsync(request, idempotencyKey, ResolveActor(), HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(GetReceipts), new { paymentId = result.PaymentId }, result);
        }
        catch (InvalidOperationException exception)
        {
            return HandleInvalidOperation(exception);
        }
    }

    private string ResolveActor()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? "unknown";
    }
}
