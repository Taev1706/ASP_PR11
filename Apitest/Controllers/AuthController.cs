using Apitest.Common;
using Apitest.Dtos;
using Apitest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Apitest.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IUserService users, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register(RegisterDto dto, CancellationToken ct)
    {
        var user = await users.RegisterAsync(dto, ct);
        return Ok(tokens.CreateAccessToken(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting(AccessRules.Login)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var user = await users.ValidateCredentialsAsync(dto, ct);
        if (user is null)
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Неверный логин или пароль." });

        return Ok(tokens.CreateAccessToken(user));
    }
}
