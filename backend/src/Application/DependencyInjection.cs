using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using SchoolClubs.Application.Services.TodoItems;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped<ITodoItemService, TodoItemService>();
    }
}
