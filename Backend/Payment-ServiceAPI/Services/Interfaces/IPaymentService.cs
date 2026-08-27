using Payment_ServiceAPI.DTOs.Payments;

namespace Payment_ServiceAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyCollection<PaymentDto>> GetPaymentsAsync(Guid? policyId, CancellationToken cancellationToken = default);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PaymentTransactionDto>> GetTransactionsAsync(Guid? paymentId, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> CreateTransactionAsync(CreatePaymentTransactionDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RefundDto>> GetRefundsAsync(Guid? paymentId, CancellationToken cancellationToken = default);
    Task<RefundDto> CreateRefundAsync(CreateRefundDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReceiptDto>> GetReceiptsAsync(Guid? paymentId, CancellationToken cancellationToken = default);
    Task<ReceiptDto> CreateReceiptAsync(CreateReceiptDto request, string idempotencyKey, string actor, string correlationId, CancellationToken cancellationToken = default);
}
