using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Application.Services.TodoItems;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddScoped<ITodoItemService, TodoItemService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAdminGroupService, AdminGroupService>();
        builder.Services.AddScoped<IAdminUserService, AdminUserService>();
        builder.Services.AddScoped<IAdminStudentService, AdminStudentService>();
    }
}
