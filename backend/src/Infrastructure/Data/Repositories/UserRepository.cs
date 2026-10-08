using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class UserRepository(DbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(email, out var address))
            return null;

        // Сравнение идёт по сконвертированному значению колонки (MailAddress.Address).
        return await DbSet.FirstOrDefaultAsync(u => u.Email == address, cancellationToken);
    }
}
