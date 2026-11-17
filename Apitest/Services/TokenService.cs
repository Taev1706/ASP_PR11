using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Apitest.Dtos;
using Microsoft.IdentityModel.Tokens;

namespace Apitest.Services;

public sealed class TokenService(IConfiguration configuration)
{
    public LoginResponseDto CreateAccessToken(UserDto user)
    {
        var jwt = configuration.GetSection("Jwt");
        var minutes = jwt.GetValue<int>("ExpiresMinutes");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };
        var credentials = new SigningCredentials(GetSigningKey(jwt), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(minutes), signingCredentials: credentials);

        return new LoginResponseDto(new JwtSecurityTokenHandler().WriteToken(token), minutes,
            user.UserId, user.Role.RoleName);
    }

    public static SymmetricSecurityKey GetSigningKey(IConfiguration jwt)
    {
        var key = Encoding.UTF8.GetBytes(jwt["Key"] ?? "");
        if (key.Length < 32)
            throw new InvalidOperationException("Задайте Jwt:Key длиной не менее 32 байт.");

        return new SymmetricSecurityKey(key);
    }
}
