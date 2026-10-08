using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class StudentRepository(DbContext dbContext) : Repository<Student>(dbContext), IStudentRepository
{
    public async Task<IReadOnlyList<Student>> ListByGroupAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        // У Student нет навигации на группы — идём со стороны Group.
        return await DbContext.Set<Group>()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Students)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> SearchAsync(string? query,
        Guid? groupId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var students = groupId is { } id
            ? DbContext.Set<Group>().Where(g => g.Id == id).SelectMany(g => g.Students)
            : DbSet.AsQueryable();

        foreach (var pattern in SearchTerms.ContainsPatterns(query))
        {
            students = students.Where(s => EF.Functions.ILike(s.LastName, pattern)
                                           || EF.Functions.ILike(s.FirstName, pattern)
                                           || EF.Functions.ILike(s.MiddleName, pattern));
        }

        return await students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> ListByNameAsync(string lastName,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        var last = lastName.Trim().ToLower();
        var first = firstName.Trim().ToLower();
        return await DbSet
            .Where(s => s.LastName.ToLower() == last && s.FirstName.ToLower() == first)
            .OrderBy(s => s.MiddleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountGradebookEntriesAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<GradebookEntry>().CountAsync(e => e.StudentId == studentId, cancellationToken);
    }
}
