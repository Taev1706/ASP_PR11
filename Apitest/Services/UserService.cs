using Apitest.Common;
using AutoMapper;
using Apitest.Dtos;
using Apitest.Models;
using Apitest.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Apitest.Services;

public class UserService(IUserRepository repository, IMapper mapper,
    ICartRepository cartRepository,
    IOrderRepository orderRepository,
    IProductReservationRepository productReservationRepository,
    IRoleRepository roleRepository, IPasswordHasher<User> passwordHasher) : Service<User, UserDto, CreateUserDto, UpdateUserDto, PatchUserDto>(repository, mapper), IUserService
{
    protected override int GetId(User entity) => entity.UserId;

    public async Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        var role = await roleRepository.GetByNameAsync(AccessRules.Customer, ct)
            ?? throw new BusinessRuleException("Роль покупателя не настроена. Выполните инициализацию ролей.");
        var login = dto.Login.Trim();
        return await CreateAsync(new CreateUserDto
        {
            Login = login,
            UserName = login,
            Password = dto.Password,
            RoleId = role.RoleId,
            IsActive = true
        }, ct);
    }

    public async Task<UserDto?> ValidateCredentialsAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await repository.GetByLoginAsync(dto.Login.Trim(), ct);
        if (user is null || !user.IsActive)
            return null;

        try
        {
            return passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password)
                == PasswordVerificationResult.Failed ? null : Mapper.Map<UserDto>(user);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public override async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        var user = Mapper.Map<User>(dto);
        await ValidateAsync(user, ct);
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
        repository.Add(user);
        await repository.SaveChangesAsync(ct);
        return await GetByIdAsync(user.UserId, ct);
    }

    public override Task UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct) =>
        SaveUserChangesAsync(id, dto, dto.Password, ct);

    public override Task PatchAsync(int id, PatchUserDto dto, CancellationToken ct) =>
        SaveUserChangesAsync(id, dto, dto.Password, ct);

    private async Task SaveUserChangesAsync<TDto>(int id, TDto dto, string? password, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException("Пользователь не найден.");
        Mapper.Map(dto, user);
        await ValidateAsync(user, ct);
        if (password is not null)
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        await repository.SaveChangesAsync(ct);
    }

    protected override async Task ValidateAsync(User entity, CancellationToken ct)
    {
        if (!await roleRepository.AnyAsync(e => e.RoleId == entity.RoleId, ct))
            throw new BusinessRuleException("Связанная запись Role не найдена.");
        if (await repository.AnyAsync(e => e.Login == entity.Login && e.UserId != entity.UserId, ct))
            throw new DataConflictException("Такой логин уже занят.");
    }

    protected override async Task ValidateDeleteAsync(User entity, CancellationToken ct)
    {
        if (await orderRepository.AnyAsync(e => e.UserId == entity.UserId, ct))
            throw new DataConflictException("Сначала удалите связанные записи Order.");
        if (await cartRepository.AnyAsync(e => e.UserId == entity.UserId, ct))
            throw new DataConflictException("Сначала удалите связанные записи Cart.");
        if (await productReservationRepository.AnyAsync(e => e.UserId == entity.UserId, ct))
            throw new DataConflictException("Сначала удалите связанные записи ProductReservation.");
    }
}
