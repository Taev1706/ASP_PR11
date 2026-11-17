using Apitest.Models;

namespace Apitest.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByLoginAsync(string login, CancellationToken ct);
}
