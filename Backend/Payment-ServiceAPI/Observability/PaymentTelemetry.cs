using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Payment_ServiceAPI.Observability;

public static class PaymentTelemetry
{
    public const string ServiceName = "Payment_ServiceAPI";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    private static readonly Meter Meter = new(ServiceName, "1.0.0");

    public static readonly Counter<long> PaymentCreateAttempts = Meter.CreateCounter<long>("payment_create_attempts");
    public static readonly Counter<long> PaymentCreateSuccess = Meter.CreateCounter<long>("payment_create_success");
    public static readonly Counter<long> PaymentCreateFailures = Meter.CreateCounter<long>("payment_create_failures");
    public static readonly Counter<long> IdempotencyHits = Meter.CreateCounter<long>("payment_idempotency_hits");
}
