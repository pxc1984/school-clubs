using Microsoft.AspNetCore.Http.HttpResults;
using SchoolClubs.Application.Services.Avatars;

namespace SchoolClubs.Web.Endpoints.v1.Avatars;

public static class Avatars
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Get);
    }

    /// <remarks>
    /// Id меняется вместе с аватаркой, поэтому ответ кешируется навсегда. <c>nosniff</c> и <c>sandbox</c> —
    /// на случай, если браузер всё же попробует трактовать картинку как документ.
    /// </remarks>
    [EndpointSummary("Avatar image")]
    public static async Task<FileContentHttpResult> Get(IAvatarService service,
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await service.GetAsync(id, cancellationToken);
        var headers = httpContext.Response.Headers;
        headers.CacheControl = "private, max-age=31536000, immutable";
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        return TypedResults.File(file.Content, file.ContentType);
    }
}
