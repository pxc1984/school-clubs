using Scalar.AspNetCore;
using SchoolClubs.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

await app.InitialiseDatabaseAsync();
await app.InitialiseStorageAsync();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment()) app.UseHsts();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

app.UseExceptionHandler(options => { });

// Фронт и API на одном origin (traefik / vite proxy) — CORS не нужен и не включается.
app.UseMiddleware<CsrfHeaderMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapEndpoints();

app.Run();

public partial class Program;
