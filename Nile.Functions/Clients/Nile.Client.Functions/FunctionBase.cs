using System.Net;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Nile.Common.Errors;
using Nile.Common.Extensions;
using Nile.Common.InternalDTOs;
using Nile.Client.Functions.Common;

namespace Nile.Client.Functions;

public abstract class FunctionBase
{
    protected const string V1Suffix = "_V1";
    protected const string V2Suffix = "_V2";
    
    private readonly IConfigUtility _configUtility;
    
    protected ILogger Logger { get; }
    
    protected FunctionBase(ILogger<FunctionBase> logger, IConfigUtility configUtility)
    {
        Logger = logger;
        _configUtility = configUtility;
    }

    protected async Task<HttpResponseData> CreateResponse(HttpRequestData req, CLI.ResponseBase response)
    {
        if (response?.Error != null)
            return await CreateErrorResponse(req, response.Error);

        if (response != null && (response.GetType() == typeof(CLI.ResponseBase) || response.GetType() == typeof(StoreUserResponseBase)))
        {
            var noContent = req.CreateResponse(HttpStatusCode.NoContent);
            CorsUtility.ApplyCorsHeaders(noContent);
            return noContent;
        }

        return await CreateJasonResponse(req, response);
    }

    private async Task<HttpResponseData> CreateJasonResponse(HttpRequestData req, CLI.ResponseBase response)
    {
        var data = req.CreateResponse(HttpStatusCode.OK);
        await data.WriteAsJsonAsync((object) response, new JsonObjectSerializer(_configUtility.JsonSerializerOptions));

        CorsUtility.ApplyCorsHeaders(data);
        return data;
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, ErrorBase error)
    {
        return error switch
        {
           ConflictError err => await CreateConflictResponse(req, err),
           ForbiddenError => await CreateForbiddenResponse(req),
           ExternalTimeoutError => await CreateGatewayTimeoutResponse(req),
           InternalError => await CreateInternalServerErrorResponse(req),
           NotFoundError err => await CreateNotFoundResponse(req, err),
           UnauthorizedError => await CreateUnauthorizedResponse(req),
           ValidationError err => await CreateValidationErrorResponse(req, err),
           _=> await CreaateInternalServerErrorResponse(req)
        };
    }

    private async Task<HttpResponseData> CreaateInternalServerErrorResponse(HttpRequestData req)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.InternalServerError,
            Title = "An unexpected error has occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        return await CreateProblemDetailsResponse(req, details, HttpStatusCode.InternalServerError);
    }

    private Task<HttpResponseData> CreateConflictResponse(HttpRequestData request, ConflictError error)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.Conflict,
            Title = "Conflict",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Detail = error.PublicMessage
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.Conflict);
    }

    private Task<HttpResponseData> CreateForbiddenResponse(HttpRequestData request)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Detail = "You do not have permission to perform this action.",
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.Forbidden);
    }

    private async Task<HttpResponseData> CreateGatewayTimeoutResponse(HttpRequestData request)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.GatewayTimeout,
            Title = "External timeout",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5"
        };

        return await CreateProblemDetailsResponse(request, details, HttpStatusCode.GatewayTimeout);
    }


    private Task<HttpResponseData> CreateInternalServerErrorResponse(HttpRequestData request)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.InternalServerError,
            Title = "An unexpected error has occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.InternalServerError);
    }

    private Task<HttpResponseData> CreateNotFoundResponse(HttpRequestData request, NotFoundError err)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.NotFound,
            Title = "Resource not found.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Detail = err.PublicMessage
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.NotFound);
    }

    private Task<HttpResponseData> CreateUnauthorizedResponse(HttpRequestData request)
    {
        var details = new ProblemDetails
        {
            Status = (int) HttpStatusCode.Unauthorized,
            Title = "Unauthorized",
            Type = "https://www.rfc-editor.org/rfc/rfc7235#section-3.1"
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.Unauthorized);
    }

    private Task<HttpResponseData> CreateValidationErrorResponse(HttpRequestData request, ValidationError error)
    {
        var details = new HttpValidationProblemDetails(error.Errors)
        {
            Status = (int) HttpStatusCode.BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        return CreateProblemDetailsResponse(request, details, HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseData> CreateProblemDetailsResponse(
        HttpRequestData request,
        ProblemDetails details,
        HttpStatusCode statusCode)
    {
        var response = request.CreateResponse();

        await response.WriteAsJsonAsync(
            (object) details,
            new JsonObjectSerializer(_configUtility.JsonSerializerOptions),
            statusCode);

        CorsUtility.ApplyCorsHeaders(response);
        return response;
    }
}