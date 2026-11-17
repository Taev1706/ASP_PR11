using Apitest.Models;
using Microsoft.EntityFrameworkCore;

namespace Apitest.Repositories;

public class RoleRepository(ShopBdContext context) : Repository<Role>(context, "RoleId"), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken ct) =>
        Context.Roles.OrderBy(role => role.RoleId)
            .FirstOrDefaultAsync(role => role.RoleName == name, ct);

    protected override IQueryable<Role> ReadQuery => Context.Set<Role>().Include(e => e.Users);
}
