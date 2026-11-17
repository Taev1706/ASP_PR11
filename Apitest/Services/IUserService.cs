using Apitest.Dtos;

namespace Apitest.Services;

public interface IUserService : IService<UserDto, CreateUserDto, UpdateUserDto, PatchUserDto>
{
    Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<UserDto?> ValidateCredentialsAsync(LoginDto dto, CancellationToken ct);
}
