using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Payment_ServiceAPI.Authentication;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Integrations.PolicyValidation;
using Payment_ServiceAPI.Middleware;
using Payment_ServiceAPI.Repositories.Implementations;
using Payment_ServiceAPI.Repositories.Interfaces;
using Payment_ServiceAPI.Services.Implementations;
using Payment_ServiceAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var paymentServiceConnectionString = builder.Configuration.GetConnectionString("PaymentServiceDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentServiceDb' was not found.");

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(paymentServiceConnectionString));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IAuditEventRepository, AuditEventRepository>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPremiumScheduleService, PremiumScheduleService>();
builder.Services.Configure<PolicyValidationOptions>(builder.Configuration.GetSection(PolicyValidationOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IPolicyValidationClient, PolicyServiceValidationClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<PolicyValidationOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }
});

builder.Services.AddPaymentServiceAuthentication(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            title = "Validation failed",
            status = StatusCodes.Status400BadRequest,
            errors,
            traceId = context.HttpContext.TraceIdentifier
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();

var publicBaseUrl = builder.Configuration["AppUrls:PublicBaseUrl"]?.TrimEnd('/');

app.UseForwardedHeaders();

app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((swagger, httpRequest) =>
    {
        var serverUrl = !string.IsNullOrWhiteSpace(publicBaseUrl)
            ? publicBaseUrl
            : $"{httpRequest.Scheme}://{httpRequest.Host.Value}";

        swagger.Servers = new List<OpenApiServer>
        {
            new()
            {
                Url = serverUrl
            }
        };
    });
});

app.UseSwaggerUI();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Payment database migration failed during startup.");
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Payment Service API" }))
    .AllowAnonymous();

app.MapHealthChecks("/health/ready");

app.MapGet("/", () => Results.Redirect("/swagger"))
    .AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;
