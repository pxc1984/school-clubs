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

if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseCors(static builder => builder
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

app.UseExceptionHandler(options => { });

app.MapEndpoints();

app.Run();

public partial class Program;
