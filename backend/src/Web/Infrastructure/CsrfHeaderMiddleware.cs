using Microsoft.AspNetCore.Mvc;

namespace SchoolClubs.Web.Infrastructure;

/// <summary>
/// Мутирующие запросы к API должны нести <c>X-Requested-With: fetch</c>. Чужой сайт не может выставить
/// этот заголовок без CORS-preflight, а CORS мы не разрешаем — вместе с SameSite=Lax это защита от CSRF.
/// </summary>
public class CsrfHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "fetch";

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresHeader(context.Request)
            && !string.Equals(context.Request.Headers[HeaderName], HeaderValue, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Missing anti-forgery header",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                Extensions = { ["code"] = "csrf_header_required" },
            });
            return;
        }

        await next(context);
    }

    private static bool RequiresHeader(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api")
               && !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
                                                     || HttpMethods.IsOptions(request.Method));
    }
}
