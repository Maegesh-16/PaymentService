using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Payment_ServiceAPI.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            _logger.LogWarning(exception, "Validation error while processing request");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Validation error", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request");
            var detail = _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.";

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Internal server error", detail);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
