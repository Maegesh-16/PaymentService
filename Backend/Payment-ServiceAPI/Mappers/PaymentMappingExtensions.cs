using Payment_ServiceAPI.DTOs.Payments;
using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Mappers;

public static class PaymentMappingExtensions
{
    public static PaymentDto ToDto(this Payment entity)
    {
        return new PaymentDto(entity.PaymentId, entity.PolicyId, entity.Amount, entity.Method, entity.Status, entity.PaymentDate);
    }

    public static PaymentTransactionDto ToDto(this PaymentTransaction entity)
    {
        return new PaymentTransactionDto(entity.TransactionId, entity.PaymentId, entity.GatewayRef, entity.Status);
    }

    public static RefundDto ToDto(this Refund entity)
    {
        return new RefundDto(entity.RefundId, entity.PaymentId, entity.Amount, entity.Status, entity.RefundDate);
    }

    public static ReceiptDto ToDto(this Receipt entity)
    {
        return new ReceiptDto(entity.ReceiptId, entity.PaymentId, entity.ReceiptNumber, entity.GeneratedDate);
    }
}
