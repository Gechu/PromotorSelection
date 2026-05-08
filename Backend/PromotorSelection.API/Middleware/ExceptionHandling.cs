using FluentValidation;
using PromotorSelection.Application.Common.Exceptions;
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

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,

            BadRequestException => StatusCodes.Status400BadRequest,

            NotFoundException => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status500InternalServerError
        };

        var message = exception.Message;

        var response = new
        {
            error = statusCode == StatusCodes.Status500InternalServerError ? "Wystąpił nieoczekiwany błąd serwera.": message
        };

        if (exception is ValidationException valEx)
        {
            var errors = valEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
            var valResponse = JsonSerializer.Serialize(new { errors });
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(valResponse);
        }

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}