using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Entities.Users;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Infrastructure.IntegrationTests.Auth;

/// <summary>Политики, которые вешаются на группы /parent, /teacher, /admin.</summary>
public class PolicyTests
{
    private static async Task<bool> AuthorizeAsync(SessionDto session, string policy)
    {
        var authorization = FunctionalTestSetup.Factory.Services.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(SessionPrincipal.Create(session), policy);
        return result.Succeeded;
    }

    private static SessionDto UserSession(bool consentRequired = false, params UserRole[] roles)
    {
        return new SessionDto(Guid.NewGuid(), roles, "stamp", consentRequired, false);
    }

    [Test]
    public async Task RolePolicies_FollowRoles()
    {
        var teacherAdmin = UserSession(false, UserRole.Teacher, UserRole.Admin);

        (await AuthorizeAsync(teacherAdmin, Policies.Teacher)).ShouldBeTrue();
        (await AuthorizeAsync(teacherAdmin, Policies.Admin)).ShouldBeTrue();
        (await AuthorizeAsync(teacherAdmin, Policies.Parent)).ShouldBeFalse();
    }

    [Test]
    public async Task RolePolicies_RequireConsent()
    {
        var parent = UserSession(true, UserRole.Parent);

        (await AuthorizeAsync(parent, Policies.Parent)).ShouldBeFalse();
        (await AuthorizeAsync(parent, Policies.Session)).ShouldBeTrue();
    }

    [Test]
    public async Task ServiceAdmin_IsAdminWithoutAccount()
    {
        var service = new SessionDto(null, [UserRole.Admin], null, false, true);

        (await AuthorizeAsync(service, Policies.Admin)).ShouldBeTrue();
        (await AuthorizeAsync(service, Policies.User)).ShouldBeFalse();
    }

    [TestCase("/parent", "/parent")]
    [TestCase("/consent?returnUrl=%2F", "/consent?returnUrl=%2F")]
    [TestCase("//evil.example", "/")]
    [TestCase("/\\evil.example", "/")]
    [TestCase("https://evil.example", "/")]
    [TestCase(null, "/")]
    public void LocalUrl_RejectsExternalRedirects(string? url, string expected)
    {
        LocalUrl.OrDefault(url).ShouldBe(expected);
    }
}
