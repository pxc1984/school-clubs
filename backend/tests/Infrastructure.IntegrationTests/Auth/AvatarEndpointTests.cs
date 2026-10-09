using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Avatars;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Data;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.IntegrationTests.Auth;

public class AvatarEndpointTests : TestBase
{
    private const string AccountAvatar = "/api/v1/account/avatar";

    private static MultipartFormDataContent File(byte[] content, string fileName = "me.png")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Test]
    public async Task Upload_Replace_Delete()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);

        var first = await client.PutAsync(AccountAvatar, File(FakeRemoteFileDownloader.Png));
        first.EnsureSuccessStatusCode();
        var firstId = (await first.Content.ReadFromJsonAsync<AvatarDto>())!.Id;
        (await client.GetFromJsonAsync<AccountDto>("/api/v1/account"))!.AvatarId.ShouldBe(firstId);

        var image = await client.GetAsync($"/api/v1/avatars/{firstId}");
        image.StatusCode.ShouldBe(HttpStatusCode.OK);
        image.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await image.Content.ReadAsByteArrayAsync()).ShouldBe(FakeRemoteFileDownloader.Png);

        // Под видом .png нельзя загрузить не-картинку.
        (await client.PutAsync(AccountAvatar, File("<svg onload=alert(1)>"u8.ToArray())))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var second = await client.PutAsync(AccountAvatar, File(FakeRemoteFileDownloader.Png));
        var secondId = (await second.Content.ReadFromJsonAsync<AvatarDto>())!.Id;
        secondId.ShouldNotBe(firstId);
        (await client.GetAsync($"/api/v1/avatars/{firstId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await client.DeleteAsync(AccountAvatar)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<AccountDto>("/api/v1/account"))!.AvatarId.ShouldBeNull();
        (await client.GetAsync($"/api/v1/avatars/{secondId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Upload_TooLarge_Returns400()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);
        var content = new byte[AvatarImages.MaxBytes + 1];
        FakeRemoteFileDownloader.Png.CopyTo(content, 0);

        (await client.PutAsync(AccountAvatar, File(content))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_OtherFileById_Returns404()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);
        var journalId = await InScope(async sp =>
        {
            var storage = sp.GetRequiredService<IFileStorage>();
            var location = storage.LocationOf($"gradebooks/{Guid.NewGuid():N}.xlsx");
            await storage.PutAsync(location, new MemoryStream(FakeRemoteFileDownloader.Png), "image/png");
            var entry = new DataEntry(location, "journal.xlsx", "image/png", FakeRemoteFileDownloader.Png.Length);
            await sp.GetRequiredService<IDataEntryRepository>().AddAsync(entry);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            return entry.Id;
        });

        (await client.GetAsync($"/api/v1/avatars/{journalId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CreateClient().GetAsync($"/api/v1/avatars/{journalId}")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }
}
