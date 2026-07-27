namespace CRM.API.Tests.Middleware;

using System.Text.Json;
using CRM.API.Middleware;
using CRM.Application.Common.Exceptions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, JsonElement Body)> InvokeWithException(Exception exceptionToThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw exceptionToThrow;
        var middleware = new ExceptionHandlingMiddleware(next);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var bodyText = await reader.ReadToEndAsync();
        var body = JsonSerializer.Deserialize<JsonElement>(bodyText);

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_Returns404_WithCorrectBody()
    {
        var (statusCode, body) = await InvokeWithException(
            new NotFoundException("Donor not found"));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task ValidationException_Returns400_WithErrorsArray()
    {
        var failures = new[] 
        { 
            new ValidationFailure("incomeTaxNumber", "Income tax number cannot start with 4.") 
        };
        
        var validationException = new CRM.Application.Common.Exceptions.ValidationException(failures);

        var (statusCode, body) = await InvokeWithException(validationException);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").GetArrayLength() > 0);
    }

    [Fact]
    public async Task UnhandledException_Returns500_WithNoStackTrace()
    {
        var (statusCode, body) = await InvokeWithException(
            new InvalidOperationException("Something exploded internally with a secret file path C:\\secrets\\db.txt"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal("INTERNAL_ERROR", body.GetProperty("code").GetString());

        var message = body.GetProperty("message").GetString();
        Assert.DoesNotContain("secret", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", message);
        Assert.DoesNotContain("InvalidOperationException", message);
    }
}
