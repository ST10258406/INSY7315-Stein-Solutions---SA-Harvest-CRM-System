namespace CRM.API.Middleware;

using System.Text.Json;
using CRM.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;

// FluentValidation defines its own ValidationException. Aliasing ours
// explicitly avoids ambiguity/wrong-type bugs if FluentValidation's
// namespace is ever in scope in this file.
using AppValidationException = CRM.Application.Common.Exceptions.ValidationException;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, code, message, errors) = MapException(exception);

        // Only log full stack traces for genuinely unexpected (500) errors.
        // 404/400/403/409 are expected control flow, not bugs — logging
        // full traces for those buries real problems in noise.
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            Log.Error(exception,
                "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
                traceId, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var envelope = new
        {
            status = statusCode,
            code,
            message,
            errors,
            traceId
        };

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Code, string Message, object? Errors) MapException(Exception exception)
    {
        return exception switch
        {
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                "NOT_FOUND",
                ex.Message,
                null),

            UnauthorizedException ex => (
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                ex.Message,
                null),

            AppValidationException ex => (
                StatusCodes.Status400BadRequest,
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                 ex.Errors is null
                    ? null
                    : (object?)ex.Errors.SelectMany(kvp => kvp.Value.Select(msg => new { field = kvp.Key, message = msg })).ToArray()),

            ForbiddenException ex => (
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                ex.Message,
                null),

            ConflictException ex => (
                StatusCodes.Status409Conflict,
                "CONFLICT",
                ex.Message,
                null),

            // Catch-all. Message is deliberately generic — the real
            // exception.Message could leak internal details (SQL errors,
            // file paths, stack info). Full detail goes to Serilog only,
            // never to the response body.
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later.",
                null)
        };
    }
}
