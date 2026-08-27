using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Payment_ServiceAPI.Authentication;

namespace Payment_ServiceAPI.Controllers;

[ApiController]
[Route("api/dev-auth")]
public class DevelopmentAuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public DevelopmentAuthController(IOptions<JwtSettings> jwtOptions, IWebHostEnvironment environment)
    {
        _jwtSettings = jwtOptions.Value;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public ActionResult<object> CreateDevelopmentToken([FromQuery] string role = PaymentServiceRoles.Administrator)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "local-swagger-tester"),
            new(JwtRegisteredClaimNames.UniqueName, "local-swagger-tester"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenValue,
            tokenType = "Bearer",
            role,
            expiresAt
        });
    }
}
