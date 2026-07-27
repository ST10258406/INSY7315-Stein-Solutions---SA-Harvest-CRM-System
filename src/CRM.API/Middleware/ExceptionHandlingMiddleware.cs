namespace CRM.API.Middleware;

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            // Minimal stub
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
