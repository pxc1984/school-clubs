using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Infrastructure.Data;
using SchoolClubs.Infrastructure.Data.Interceptors;
using SchoolClubs.Infrastructure.Data.Repositories;
using SchoolClubs.Infrastructure.Identity;
using SchoolClubs.Infrastructure.Spreadsheets;
using SchoolClubs.Infrastructure.Storage;

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
        builder.Services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddSingleton<IGradebookSpreadsheet, ClosedXmlGradebookSpreadsheet>();
        builder.AddFileStorage();

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();
    }

    private static void AddFileStorage(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<S3Options>(builder.Configuration.GetSection(S3Options.SectionName));
        builder.Services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle,
                // RustFS и многие S3-совместимые сервисы не понимают новые обязательные контрольные суммы SDK v4.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            };
            if (string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
            }
            else
            {
                config.ServiceURL = options.ServiceUrl;
                config.AuthenticationRegion = options.Region;
            }

            return string.IsNullOrEmpty(options.AccessKey)
                ? new AmazonS3Client(config)
                : new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });
        builder.Services.AddSingleton<S3FileStorage>();
        builder.Services.AddSingleton<IFileStorage>(provider => provider.GetRequiredService<S3FileStorage>());
    }
}
