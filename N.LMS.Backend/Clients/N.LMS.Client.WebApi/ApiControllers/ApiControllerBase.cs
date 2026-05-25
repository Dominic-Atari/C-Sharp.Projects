using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using N.LMS.Client.WebApi.Response;
using N.LMS.Common.Interface.Errors;

namespace N.LMS.Client.WebApi.ApiControllers;

public abstract class ApiControllerBase : ControllerBase
{
    internal static readonly Mapper Mapper = new();

    private static readonly JsonSerializerOptions _SerializerOptions = new(JsonSerializerDefaults.Web);

    protected IActionResult CreateActionResult(ResponseBase response)
    {
        if (response.Errors is null || response.Errors.Length == 0)
        {
            return new ContentResult
            {
                Content = JsonSerializer.Serialize<object>(response, _SerializerOptions),
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }

        return response.Errors[0] switch
        {
            ValidationError ve   => BadRequestProblem(ve, response.Errors),
            UnauthorizedError ue => Unauthorized(new { ue.Code, ue.PublicMessage }),
            NotFoundError nf     => NotFound(new { nf.EntityName, nf.FieldName, nf.FieldValue, nf.PublicMessage }),
            ConflictError ce     => Conflict(new { ce.EntityName, ce.FieldName, ce.PublicMessage }),
            _                    => InternalServerErrorProblem(response.Errors)
        };
    }

    protected IActionResult CreateActionResult(Exception ex) =>
        Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);

    private IActionResult BadRequestProblem(ValidationError first, ErrorBase[] all)
    {
        var dict = new Dictionary<string, string[]>();
        foreach (var err in all.OfType<ValidationError>())
        {
            var key = err.FieldName ?? string.Empty;
            var msg = err.PublicMessage ?? string.Empty;
            dict[key] = dict.TryGetValue(key, out var existing) ? [.. existing, msg] : [msg];
        }
        return ValidationProblem(new ValidationProblemDetails(dict)
        {
            Title = first.PublicMessage ?? "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    private IActionResult InternalServerErrorProblem(ErrorBase[] errors) =>
        Problem(
            detail: errors[0].PublicMessage,
            statusCode: StatusCodes.Status500InternalServerError);
}
