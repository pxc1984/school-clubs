using SchoolClubs.Infrastructure.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddOpenApi();

        builder.Services.AddCors();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();
    }
}
