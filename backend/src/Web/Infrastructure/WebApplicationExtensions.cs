using SchoolClubs.Web.Endpoints.v1.Account;
using SchoolClubs.Web.Endpoints.v1.Admin;
using SchoolClubs.Web.Endpoints.v1.Avatars;
using SchoolClubs.Web.Endpoints.v1.Parent;
using SchoolClubs.Web.Endpoints.v1.Teacher;
using SchoolClubs.Web.Endpoints.v1.TodoItems;
using SchoolClubs.Web.Infrastructure.Auth;
using AuthEndpoints = SchoolClubs.Web.Endpoints.v1.Auth.Auth;

namespace SchoolClubs.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1");

        TodoItems.Map(v1
            .MapGroup("/todo-items")
            .WithTags("TodoItems"));

        AuthEndpoints.Map(v1
            .MapGroup("/auth")
            .WithTags("Auth"));

        Account.Map(v1
            .MapGroup("/account")
            .RequireAuthorization(Policies.User)
            .WithTags("Account"));

        Avatars.Map(v1
            .MapGroup("/avatars")
            .RequireAuthorization(Policies.Consent)
            .WithTags("Avatars"));

        // Вход сервисного админа анонимный; остальные /admin/* — под политикой Admin.
        var admin = v1.MapGroup("/admin");
        AdminAuth.Map(admin.MapGroup("").WithTags("Admin"));
        AdminGroups.Map(admin
            .MapGroup("/groups")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: groups"));
        AdminUsers.Map(admin
            .MapGroup("/users")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: users"));
        AdminUsers.MapTeachers(admin
            .MapGroup("/teachers")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: users"));
        AdminUsers.MapParents(admin
            .MapGroup("/parents")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: users"));
        AdminAccessRequests.Map(admin
            .MapGroup("/access-requests")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: access requests"));
        AdminStudents.Map(admin
            .MapGroup("/students")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin: students"));

        var parent = v1.MapGroup("/parent").RequireAuthorization(Policies.Parent);
        ParentAccessRequests.Map(parent
            .MapGroup("")
            .WithTags("Parent: access requests"));
        ParentChildren.Map(parent
            .MapGroup("/children")
            .WithTags("Parent: children"));

        var teacher = v1.MapGroup("/teacher").RequireAuthorization(Policies.Teacher);
        TeacherGroups.Map(teacher
            .MapGroup("/groups")
            .WithTags("Teacher: groups"));
        TeacherLessons.Map(teacher
            .MapGroup("/lessons")
            .WithTags("Teacher: lessons"));

        return app;
    }
}
