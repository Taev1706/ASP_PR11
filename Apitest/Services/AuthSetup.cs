using System.ComponentModel.DataAnnotations;
using Apitest.Common;
using Apitest.Dtos;
using Apitest.Models;
using Apitest.Repositories;

namespace Apitest.Services;

public static class AuthSetup
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roles = services.GetRequiredService<IRoleRepository>();
        var admin = await roles.GetByNameAsync(AccessRules.Admin, CancellationToken.None);
        var customer = await roles.GetByNameAsync(AccessRules.Customer, CancellationToken.None);
        if (admin is null)
        {
            admin = new Role { RoleName = AccessRules.Admin };
            roles.Add(admin);
        }
        if (customer is null)
            roles.Add(new Role { RoleName = AccessRules.Customer });
        await roles.SaveChangesAsync(CancellationToken.None);

        var login = configuration["AuthSetup:AdminLogin"]?.Trim();
        if (string.IsNullOrEmpty(login))
            return;

        var users = services.GetRequiredService<IUserRepository>();
        var existing = await users.GetByLoginAsync(login, CancellationToken.None);
        if (existing is not null)
        {
            if (existing.Role.RoleName != AccessRules.Admin || !existing.IsActive)
                throw new InvalidOperationException("Этот логин уже занят или учётная запись администратора неактивна.");
            Console.WriteLine("Учётная запись администратора уже существует.");
            return;
        }

        var dto = new CreateUserDto
        {
            Login = login,
            UserName = login,
            Password = configuration["AuthSetup:AdminPassword"] ?? "",
            RoleId = admin.RoleId,
            IsActive = true
        };
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);
        await services.GetRequiredService<IUserService>().CreateAsync(dto, CancellationToken.None);
        Console.WriteLine("Учётная запись администратора создана.");
    }
}
