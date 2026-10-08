using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Infrastructure.Data;
using SchoolClubs.Infrastructure.Data.Interceptors;
using SchoolClubs.Infrastructure.Data.Repositories;
using SchoolClubs.Infrastructure.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            options.UseNpgsql(connectionString);
        });

        // Репозитории принимают базовый DbContext; маппим его на конкретный контекст.
        builder.Services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<ITodoItemRepository, TodoItemRepository>();
        builder.Services.AddScoped<IDataEntryRepository, DataEntryRepository>();
        builder.Services.AddScoped<IGroupRepository, GroupRepository>();
        builder.Services.AddScoped<ILessonRepository, LessonRepository>();
        builder.Services.AddScoped<ILessonGradebookRepository, LessonGradebookRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        builder.Services.AddScoped<IStudentRepository, StudentRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();
    }
}
