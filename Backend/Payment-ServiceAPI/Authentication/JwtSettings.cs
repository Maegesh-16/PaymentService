namespace Payment_ServiceAPI.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;

    public int ClockSkewMinutes { get; set; } = 5;
}
