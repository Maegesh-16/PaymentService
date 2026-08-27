using Payment_ServiceAPI.DTOs.Payments;
using Payment_ServiceAPI.Integrations.PolicyValidation;
using Payment_ServiceAPI.Mappers;
using Payment_ServiceAPI.Models.Audit;
using Payment_ServiceAPI.Models.Operations;
using Payment_ServiceAPI.Models.Payments;
using Payment_ServiceAPI.Observability;
using Payment_ServiceAPI.Repositories.Interfaces;
using Payment_ServiceAPI.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Payment_ServiceAPI.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IPolicyValidationClient _policyValidationClient;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentTransactionRepository paymentTransactionRepository,
        IRefundRepository refundRepository,
        IReceiptRepository receiptRepository,
        IPolicyValidationClient policyValidationClient,
        IIdempotencyRepository idempotencyRepository,
        IAuditEventRepository auditEventRepository,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _refundRepository = refundRepository;
        _receiptRepository = receiptRepository;
        _policyValidationClient = policyValidationClient;
        _idempotencyRepository = idempotencyRepository;
        _auditEventRepository = auditEventRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<PaymentDto>> GetPaymentsAsync(Guid? policyId, CancellationToken cancellationToken = default)
    {
        using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.get");
        activity?.SetTag("policy.id", policyId?.ToString());

        var payments = await _paymentRepository.GetAsync(policyId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} payments for policy filter {PolicyId}", payments.Count, policyId);
        return payments.Select(x => x.ToDto()).ToList();
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteIdempotentAsync(
            operation: "CreatePayment",
            idempotencyKey: idempotencyKey,
            request: request,
            actor: actor,
            action: async () =>
            {
                PaymentTelemetry.PaymentCreateAttempts.Add(1);
                using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.create");
                activity?.SetTag("policy.id", request.PolicyId.ToString());

                await EnsurePolicyExistsAsync(request.PolicyId, cancellationToken);

                var entity = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    PolicyId = request.PolicyId,
                    Amount = request.Amount,
                    Method = request.Method,
                    Status = request.Status,
                    PaymentDate = request.PaymentDate,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = actor
                };

                await _paymentRepository.AddAsync(entity, cancellationToken);
                await WriteAuditEventAsync("PaymentCreated", nameof(Payment), entity.PaymentId, actor, correlationId, new
                {
                    entity.PolicyId,
                    entity.Amount,
                    entity.Method,
                    entity.Status,
                    entity.PaymentDate
                }, cancellationToken);

                PaymentTelemetry.PaymentCreateSuccess.Add(1);
                _logger.LogInformation("Payment created. PaymentId={PaymentId} PolicyId={PolicyId} Actor={Actor}", entity.PaymentId, entity.PolicyId, actor);
                return entity.ToDto();
            },
            onFailure: exception =>
            {
                PaymentTelemetry.PaymentCreateFailures.Add(1);
                _logger.LogWarning(exception, "Create payment failed for policy {PolicyId}", request.PolicyId);
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentTransactionDto>> GetTransactionsAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.transactions.get");
        activity?.SetTag("payment.id", paymentId?.ToString());

        var transactions = await _paymentTransactionRepository.GetAsync(paymentId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} transactions for payment filter {PaymentId}", transactions.Count, paymentId);
        return transactions.Select(x => x.ToDto()).ToList();
    }

    public async Task<PaymentTransactionDto> CreateTransactionAsync(CreatePaymentTransactionDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteIdempotentAsync(
            operation: "CreateTransaction",
            idempotencyKey: idempotencyKey,
            request: request,
            actor: actor,
            action: async () =>
            {
                using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.transactions.create");
                activity?.SetTag("payment.id", request.PaymentId.ToString());

                await EnsurePaymentExistsAsync(request.PaymentId, cancellationToken);

                if (await _paymentTransactionRepository.GatewayRefExistsAsync(request.GatewayRef, cancellationToken))
                {
                    throw new InvalidOperationException($"Gateway reference '{request.GatewayRef}' already exists.");
                }

                var entity = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    GatewayRef = request.GatewayRef,
                    Status = request.Status,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = actor
                };

                await _paymentTransactionRepository.AddAsync(entity, cancellationToken);
                await WriteAuditEventAsync("PaymentTransactionCreated", nameof(PaymentTransaction), entity.TransactionId, actor, correlationId, new
                {
                    entity.PaymentId,
                    entity.GatewayRef,
                    entity.Status
                }, cancellationToken);

                _logger.LogInformation("Transaction created. TransactionId={TransactionId} PaymentId={PaymentId} Actor={Actor}", entity.TransactionId, entity.PaymentId, actor);
                return entity.ToDto();
            },
            onFailure: exception => _logger.LogWarning(exception, "Create transaction failed for payment {PaymentId}", request.PaymentId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<RefundDto>> GetRefundsAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.refunds.get");
        activity?.SetTag("payment.id", paymentId?.ToString());

        var refunds = await _refundRepository.GetAsync(paymentId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} refunds for payment filter {PaymentId}", refunds.Count, paymentId);
        return refunds.Select(x => x.ToDto()).ToList();
    }

    public async Task<RefundDto> CreateRefundAsync(CreateRefundDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteIdempotentAsync(
            operation: "CreateRefund",
            idempotencyKey: idempotencyKey,
            request: request,
            actor: actor,
            action: async () =>
            {
                using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.refunds.create");
                activity?.SetTag("payment.id", request.PaymentId.ToString());

                await EnsurePaymentExistsAsync(request.PaymentId, cancellationToken);

                var entity = new Refund
                {
                    RefundId = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    Amount = request.Amount,
                    Status = request.Status,
                    RefundDate = request.RefundDate,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = actor
                };

                await _refundRepository.AddAsync(entity, cancellationToken);
                await WriteAuditEventAsync("RefundCreated", nameof(Refund), entity.RefundId, actor, correlationId, new
                {
                    entity.PaymentId,
                    entity.Amount,
                    entity.Status,
                    entity.RefundDate
                }, cancellationToken);

                _logger.LogInformation("Refund created. RefundId={RefundId} PaymentId={PaymentId} Actor={Actor}", entity.RefundId, entity.PaymentId, actor);
                return entity.ToDto();
            },
            onFailure: exception => _logger.LogWarning(exception, "Create refund failed for payment {PaymentId}", request.PaymentId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReceiptDto>> GetReceiptsAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.receipts.get");
        activity?.SetTag("payment.id", paymentId?.ToString());

        var receipts = await _receiptRepository.GetAsync(paymentId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} receipts for payment filter {PaymentId}", receipts.Count, paymentId);
        return receipts.Select(x => x.ToDto()).ToList();
    }

    public async Task<ReceiptDto> CreateReceiptAsync(CreateReceiptDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteIdempotentAsync(
            operation: "CreateReceipt",
            idempotencyKey: idempotencyKey,
            request: request,
            actor: actor,
            action: async () =>
            {
                using var activity = PaymentTelemetry.ActivitySource.StartActivity("payment.receipts.create");
                activity?.SetTag("payment.id", request.PaymentId.ToString());

                await EnsurePaymentExistsAsync(request.PaymentId, cancellationToken);

                if (await _receiptRepository.ReceiptNumberExistsAsync(request.ReceiptNumber, cancellationToken))
                {
                    throw new InvalidOperationException($"Receipt number '{request.ReceiptNumber}' already exists.");
                }

                var entity = new Receipt
                {
                    ReceiptId = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    ReceiptNumber = request.ReceiptNumber,
                    GeneratedDate = request.GeneratedDate,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = actor
                };

                await _receiptRepository.AddAsync(entity, cancellationToken);
                await WriteAuditEventAsync("ReceiptCreated", nameof(Receipt), entity.ReceiptId, actor, correlationId, new
                {
                    entity.PaymentId,
                    entity.ReceiptNumber,
                    entity.GeneratedDate
                }, cancellationToken);

                _logger.LogInformation("Receipt created. ReceiptId={ReceiptId} PaymentId={PaymentId} Actor={Actor}", entity.ReceiptId, entity.PaymentId, actor);
                return entity.ToDto();
            },
            onFailure: exception => _logger.LogWarning(exception, "Create receipt failed for payment {PaymentId}", request.PaymentId),
            cancellationToken: cancellationToken);
    }

    private async Task<TResponse> ExecuteIdempotentAsync<TRequest, TResponse>(
        string operation,
        string idempotencyKey,
        TRequest request,
        string actor,
        Func<Task<TResponse>> action,
        Action<Exception> onFailure,
        CancellationToken cancellationToken)
    {
        var requestHash = ComputeHash(JsonSerializer.Serialize(request));

        var existing = await _idempotencyRepository.GetAsync(operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Idempotency key '{idempotencyKey}' was already used with a different payload for operation '{operation}'.");
            }

            var cached = JsonSerializer.Deserialize<TResponse>(existing.ResponsePayload);
            if (cached is null)
            {
                throw new InvalidOperationException("Idempotent response could not be restored.");
            }

            PaymentTelemetry.IdempotencyHits.Add(1);
            _logger.LogInformation("Idempotent replay for {Operation}. Key={IdempotencyKey} Actor={Actor}", operation, idempotencyKey, actor);
            return cached;
        }

        try
        {
            var response = await action();

            await _idempotencyRepository.AddAsync(new IdempotencyRecord
            {
                IdempotencyRecordId = Guid.NewGuid(),
                Operation = operation,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
                ResponsePayload = JsonSerializer.Serialize(response),
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await _paymentRepository.SaveChangesAsync(cancellationToken);
            return response;
        }
        catch (Exception exception)
        {
            onFailure(exception);
            throw;
        }
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private async Task WriteAuditEventAsync(
        string eventType,
        string entityName,
        Guid entityId,
        string actor,
        string correlationId,
        object details,
        CancellationToken cancellationToken)
    {
        await _auditEventRepository.AddAsync(new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            EventType = eventType,
            EntityName = entityName,
            EntityId = entityId.ToString(),
            Actor = actor,
            CorrelationId = correlationId,
            Details = JsonSerializer.Serialize(details),
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsurePaymentExistsAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            throw new InvalidOperationException($"Payment '{paymentId}' was not found.");
        }
    }

    private async Task EnsurePolicyExistsAsync(Guid policyId, CancellationToken cancellationToken)
    {
        var exists = await _policyValidationClient.PolicyExistsAsync(policyId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"Policy '{policyId}' was not found.");
        }
    }
}
