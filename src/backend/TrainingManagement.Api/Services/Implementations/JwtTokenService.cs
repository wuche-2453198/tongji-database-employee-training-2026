using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Auth;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>生成带身份、角色、权限和有效期的 HMAC-SHA256 JWT；签名用于防篡改，不用于加密资料。</summary>
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(AuthUserResponse user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(Math.Max(1, _options.ExpireMinutes));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.EmpId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.EmpId.ToString()),
            new(TrainingClaimTypes.EmployeeId, user.EmpId.ToString()),
            new(ClaimTypes.Name, user.EmpName)
        };

        AddIfNotEmpty(claims, ClaimTypes.Email, user.Email);
        AddIfNotEmpty(claims, TrainingClaimTypes.Department, user.DeptName);
        AddIfNotEmpty(claims, TrainingClaimTypes.Position, user.Position);

        foreach (var roleCode in user.Roles.Select(role => role.RoleCode).Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
        }

        foreach (var permission in user.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(TrainingClaimTypes.Permission, permission));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>仅为非空资料添加令牌声明，避免写入无意义的空值。</summary>
    private static void AddIfNotEmpty(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }
}
