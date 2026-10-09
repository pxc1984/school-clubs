using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SchoolClubs.Web.Infrastructure;

public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest,
                (ProblemDetails) new ValidationProblemDetails(ve.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                }),
            NotFoundException ne => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "The specified resource was not found.",
                Detail = ne.Message,
            }),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            }),
            ForbiddenAccessException fe => (StatusCodes.Status403Forbidden, WithCode(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            }, fe.Code)),
            ConflictException ce => (StatusCodes.Status409Conflict, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = ce.Message,
            }),
            AccessRequestStateException se => (StatusCodes.Status409Conflict, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = se.Message,
            }),
            // Нарушение доменных инвариантов (сущности бросают ArgumentException и наследников).
            ArgumentException ae => (StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad request",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = ae.Message,
            }),
            _ => (-1, null),
        };

        if (problemDetails is null) return false;

        httpContext.Response.StatusCode = statusCode;
        // runtime-тип, иначе System.Text.Json отбросит Errors у ValidationProblemDetails
        await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), cancellationToken);
        return true;
    }

    private static ProblemDetails WithCode(ProblemDetails problemDetails, string? code)
    {
        if (code is not null)
            problemDetails.Extensions["code"] = code;
        return problemDetails;
    }
}
