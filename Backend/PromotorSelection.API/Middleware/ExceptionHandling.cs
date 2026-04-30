using FluentValidation;
using System.Text.Json;

namespace PromotorSelection.API.Middleware;

public class ExceptionHandling
{
    private readonly RequestDelegate _next;

    public ExceptionHandling(RequestDelegate next)
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = StatusCodes.Status500InternalServerError;
        var result = "";

        if (exception is ValidationException validationException)
        {
            statusCode = StatusCodes.Status400BadRequest;

            var errors = validationException.Errors
                .Select(e => new { e.PropertyName, e.ErrorMessage });

            result = JsonSerializer.Serialize(new { errors });
        }
        else
        {
            result = JsonSerializer.Serialize(new { error = "Wystąpił nieoczekiwany błąd serwera." });
        }

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(result);
    }
}