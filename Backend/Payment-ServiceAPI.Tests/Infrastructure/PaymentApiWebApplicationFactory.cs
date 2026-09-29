using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payment_ServiceAPI.Integrations.PolicyValidation;

namespace Payment_ServiceAPI.Tests.Infrastructure;

public class PaymentApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _overrides;
    private readonly string _testDbName;

    public PaymentApiWebApplicationFactory(Dictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _testDbName = $"InsurancePaymentApiTests_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PaymentServiceDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database={_testDbName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True",
                ["PolicyValidation:BaseUrl"] = string.Empty,
                ["PolicyValidation:AllowFallbackKnownPolicies"] = "true",
                ["PolicyValidation:KnownPolicyIds:0"] = "11111111-1111-1111-1111-111111111111"
            });

            if (_overrides.Count > 0)
            {
                configurationBuilder.AddInMemoryCollection(_overrides);
            }
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPolicyValidationClient>();
            services.AddSingleton<IPolicyValidationClient, TestPolicyValidationClient>();
        });
    }

    private sealed class TestPolicyValidationClient : IPolicyValidationClient
    {
        private static readonly Guid KnownPolicyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(policyId == KnownPolicyId);

        public Task<PolicyPaymentDetails?> GetPolicyPaymentDetailsAsync(Guid policyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(policyId == KnownPolicyId
                ? new PolicyPaymentDetails(KnownPolicyId, new DateTime(2026, 9, 1), 9311m, 3)
                : null);
    }
}
