using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Payment_ServiceAPI.Integrations.PolicyValidation;

public class PolicyServiceValidationClient : IPolicyValidationClient
{
    private static readonly object CircuitLock = new();
    private static int _consecutiveFailures;
    private static DateTimeOffset? _circuitOpenUntilUtc;

    private readonly HttpClient _httpClient;
    private readonly ILogger<PolicyServiceValidationClient> _logger;
    private readonly PolicyValidationOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PolicyServiceValidationClient(
        HttpClient httpClient,
        IOptions<PolicyValidationOptions> options,
        ILogger<PolicyServiceValidationClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;

        if (_options.RequestTimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        }
    }

    public async Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        if (policyId == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            if (_options.AllowFallbackKnownPolicies)
            {
                return _options.KnownPolicyIds.Contains(policyId);
            }

            throw new InvalidOperationException("PolicyValidation BaseUrl is not configured.");
        }

        if (IsCircuitOpen())
        {
            _logger.LogWarning("Policy validation circuit is open. Falling back to known policies for {PolicyId}", policyId);
            return _options.AllowFallbackKnownPolicies && _options.KnownPolicyIds.Contains(policyId);
        }

        var endpoint = _options.ExistsEndpointTemplate.Replace("{policyId}", policyId.ToString());

        for (var attempt = 1; attempt <= _options.RetryCount + 1; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
                if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization))
                {
                    request.Headers.Authorization = authorization;
                }
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    RecordSuccess();
                    return false;
                }

                if (response.IsSuccessStatusCode)
                {
                    RecordSuccess();
                    return await ParseSuccessResponseAsync(response, cancellationToken);
                }

                if (IsTransient(response.StatusCode))
                {
                    throw new HttpRequestException($"Policy service transient error: {(int)response.StatusCode}");
                }

                RecordSuccess();
                return false;
            }
            catch (Exception exception) when (IsRetryable(exception) && attempt <= _options.RetryCount)
            {
                RecordFailure();
                var backoff = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));
                _logger.LogWarning(exception, "Policy validation retry {Attempt} for {PolicyId}", attempt, policyId);
                await Task.Delay(backoff, cancellationToken);
            }
            catch (Exception exception) when (IsRetryable(exception))
            {
                RecordFailure();
                _logger.LogError(exception, "Policy validation failed after retries for {PolicyId}", policyId);

                if (_options.AllowFallbackKnownPolicies)
                {
                    return _options.KnownPolicyIds.Contains(policyId);
                }

                throw;
            }
        }

        return false;
    }

    public async Task<PolicyPaymentDetails?> GetPolicyPaymentDetailsAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        if (policyId == Guid.Empty || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        var endpoint = _options.ExistsEndpointTemplate.Replace("{policyId}", policyId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization))
        {
            request.Headers.Authorization = authorization;
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolicyPaymentDetails>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken);
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code == 408 || code == 429 || code >= 500;
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception is HttpRequestException or TaskCanceledException;
    }

    private async Task<bool> ParseSuccessResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content == null)
        {
            return true;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (document.RootElement.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("exists", out var existsElement) &&
                (existsElement.ValueKind == JsonValueKind.True || existsElement.ValueKind == JsonValueKind.False))
            {
                return existsElement.GetBoolean();
            }
        }
        catch (JsonException)
        {
            // The endpoint may return a full policy document. Success means policy exists.
        }

        return true;
    }

    private bool IsCircuitOpen()
    {
        lock (CircuitLock)
        {
            return _circuitOpenUntilUtc.HasValue && _circuitOpenUntilUtc.Value > DateTimeOffset.UtcNow;
        }
    }

    private void RecordSuccess()
    {
        lock (CircuitLock)
        {
            _consecutiveFailures = 0;
            _circuitOpenUntilUtc = null;
        }
    }

    private void RecordFailure()
    {
        lock (CircuitLock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _options.CircuitBreakerFailureThreshold)
            {
                _circuitOpenUntilUtc = DateTimeOffset.UtcNow.AddSeconds(_options.CircuitBreakerDurationSeconds);
                _consecutiveFailures = 0;
            }
        }
    }
}
