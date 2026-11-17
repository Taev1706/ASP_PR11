using Apitest.Models;
using Microsoft.EntityFrameworkCore;

namespace Apitest.Repositories;

public class UserRepository(ShopBdContext context) : Repository<User>(context, "UserId"), IUserRepository
{
    public Task<User?> GetByLoginAsync(string login, CancellationToken ct) =>
        Context.Users.AsNoTracking().Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Login == login, ct);

    protected override IQueryable<User> ReadQuery => Context.Set<User>().Include(e => e.Role).Include(e => e.Cart).Include(e => e.Orders);
}
