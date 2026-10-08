using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Entities;

namespace SchoolClubs.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems
    {
        get { return Set<TodoItem>(); }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
