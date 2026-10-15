using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Api.Security;

public static class ProblemDetailsFactory
{
    public static ObjectResult Create(int statusCode, string title, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
