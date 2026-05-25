using System.Text.Json;
using N.LMS.Common.Interface.Exceptions;

namespace N.LMS.Client.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await Write(context, StatusCodes.Status404NotFound, new
            {
                ex.EntityName,
                ex.FieldName,
                ex.FieldValue,
                ex.Message
            });
        }
        catch (ConflictException ex)
        {
            await Write(context, StatusCodes.Status409Conflict, new
            {
                ex.EntityName,
                ex.FieldName,
                ex.Message
            });
        }
        catch (ValidationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, new
            {
                ex.FieldName,
                ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request pipeline.");
            await Write(context, StatusCodes.Status500InternalServerError, new
            {
                Message = "An unexpected error occurred."
            });
        }
    }

    private static Task Write(HttpContext context, int statusCode, object payload)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
