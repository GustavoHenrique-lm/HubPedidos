
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HubPedidos.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HubPedidos.Api.Authentication;

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _settings;

    public JwtTokenService(
        IConfiguration configuration,
        IOptions<JwtSettings> settings)
    {
        _configuration = configuration;
        _settings = settings.Value;
    }

    public string GerarToken(ApplicationUser usuario)
    {
        var chave = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chave JWT não configurada.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? ""),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.UserName ?? "")
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(chave));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
