using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SchoolClubs.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }

    /// <summary>Создаёт бакет, если это разрешено конфигурацией (<c>Storage:S3:CreateBucket</c>).</summary>
    public static async Task InitialiseStorageAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Storage.S3Options>>();
        if (options.Value.CreateBucket)
            await app.Services.GetRequiredService<Storage.S3FileStorage>().EnsureBucketAsync();
    }
}

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    ApplicationDbContext db)
{
    public async Task InitialiseAsync()
    {
        try
        {
            // Пока миграций нет — схема создаётся из модели; после первой миграции применяются они.
            if ((await db.Database.GetPendingMigrationsAsync()).Any())
                await db.Database.MigrateAsync();
            else
                await db.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        if (await db.TodoItems.AnyAsync()) return;

        db.TodoItems.Add(new Domain.Entities.TodoItem { Title = "Rename the schoolclubs" });
        await db.SaveChangesAsync();
    }
}
