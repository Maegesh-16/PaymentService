using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Payment_ServiceAPI.DTOs.Payments;
using Payment_ServiceAPI.Tests.Infrastructure;
using Xunit;

namespace Payment_ServiceAPI.Tests;

public class PaymentApiIntegrationTests
{
    private static readonly Guid KnownPolicyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetPayments_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new PaymentApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithDuplicateReceiptNumber_ReturnsBadRequest()
    {
        await using var factory = new PaymentApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthorizeAsAdminAsync(client);

        var payment = await CreatePaymentAsync(client);

        var receiptNumber = $"R-{Guid.NewGuid():N}";
        var receiptBody = new CreateReceiptDto(payment.PaymentId, receiptNumber, DateTime.UtcNow);

        var first = await PostWithIdempotencyAsync(client, "/api/payments/receipts", receiptBody, $"receipt-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await PostWithIdempotencyAsync(client, "/api/payments/receipts", receiptBody, $"receipt-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithUnknownPolicy_ReturnsBadRequest()
    {
        await using var factory = new PaymentApiWebApplicationFactory(new Dictionary<string, string?>
        {
            ["PolicyValidation:KnownPolicyIds:0"] = null
        });

        using var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var body = new CreatePaymentDto(Guid.NewGuid(), 1000m, "UPI", "Pending", DateTime.UtcNow);
        var response = await PostWithIdempotencyAsync(client, "/api/payments", body, "payment-unknown-policy");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthorizeAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/dev-auth/token?role=Administrator", null);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var token = json.RootElement.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<PaymentDto> CreatePaymentAsync(HttpClient client)
    {
        var body = new CreatePaymentDto(KnownPolicyId, 2500m, "Card", "Completed", DateTime.UtcNow);
        var response = await PostWithIdempotencyAsync(client, "/api/payments", body, $"payment-create-{Guid.NewGuid()}");

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreatePayment failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {errorBody}");
        }

        var created = await response.Content.ReadFromJsonAsync<PaymentDto>();

        Assert.NotNull(created);
        return created!;
    }

    private static Task<HttpResponseMessage> PostWithIdempotencyAsync<T>(HttpClient client, string url, T body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }
}
