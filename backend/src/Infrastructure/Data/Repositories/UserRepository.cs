using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class UserRepository(DbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Email в User хранится в нижнем регистре (см. User.ChangeEmail).
        if (!MailAddress.TryCreate(email.Trim().ToLowerInvariant(), out var address))
            return null;

        // Сравнение идёт по сконвертированному значению колонки (MailAddress.Address).
        return await DbSet.FirstOrDefaultAsync(u => u.Email == address, cancellationToken);
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (User.NormalizeUsername(username) is not { } normalized)
            return null;

        return await DbSet.FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
    }

    public async Task<User?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Children)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetWithGroupsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.LinkedGroups)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetWithLinksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Children)
            .Include(u => u.LinkedGroups)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListByChildAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Children)
            .Where(u => u.Children.Any(c => c.Id == studentId))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role,
        CancellationToken cancellationToken = default)
    {
        // Роли — owned-коллекция на поле _roles, публичное свойство Roles в запросах не транслируется.
        return await DbSet
            .Where(u => EF.Property<List<UserRoleGrant>>(u, "_roles").Any(r => r.Role == role))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(string? query,
        UserRole? role,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var users = DbSet.AsQueryable();
        if (role is { } r)
            users = users.Where(u => EF.Property<List<UserRoleGrant>>(u, "_roles").Any(g => g.Role == r));

        foreach (var word in SearchTerms.Words(query))
        {
            // Email хранится через конвертер MailAddress, подстрочный ILIKE по нему не транслируется —
            // поэтому email ищем точным совпадением.
            if (word.Contains('@'))
            {
                if (!MailAddress.TryCreate(word.ToLowerInvariant(), out var address))
                    return [];
                users = users.Where(u => u.Email == address);
                continue;
            }

            var pattern = SearchTerms.ContainsPatterns(word)[0];
            users = users.Where(u => EF.Functions.ILike(u.LastName, pattern)
                                     || EF.Functions.ILike(u.FirstName, pattern)
                                     || EF.Functions.ILike(u.MiddleName, pattern));
        }

        return await users
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
