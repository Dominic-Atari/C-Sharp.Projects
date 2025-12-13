using Microsoft.Extensions.Logging;
using Nile.Common.Errors;
using Nile.Common.Exceptions;
using Nile.Utilities;

namespace Nile.Managers.Proxys;

public class Proxy<TManager> : IProxy<TManager> where TManager : notnull
{
    private readonly TManager _manager;

    private readonly IContextFactoryUtility _contextFactoryUtility;

    private readonly ILogger<Proxy<TManager>> _logger;

    private readonly IAuthorizer _authorizer;

    private readonly IConverter _converter;

    private readonly IValidator _validator;
    
    public Proxy(TManager manager, IContextFactoryUtility contextFactoryUtility, ILogger<Proxy<TManager>> logger, IAuthorizer authorizer, IConverter converter, IValidator validator)
    {
        _manager = manager;
        _contextFactoryUtility = contextFactoryUtility;
        _logger = logger;
        _authorizer = authorizer;
        _converter = converter;
        _validator = validator;
    }

    Task<TResponse> IProxy<TManager>.RunWithoutRequestBody<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc)
    {
        return Run<TRequest, TResponse>(getFunc, requestStream: null, requestDto: default, routeParams: null);
    }

    Task<TResponse> IProxy<TManager>.RunWithRequestDto<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        TRequest request)
    {
        return Run<TRequest, TResponse>(getFunc, requestStream: null, requestDto: request, routeParams: null);
    }

    Task<TResponse> IProxy<TManager>.RunWithRequestStream<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        System.IO.Stream requestBody)
    {
        return Run<TRequest, TResponse>(getFunc, requestStream: requestBody, requestDto: default, routeParams: null);
    }

    Task<TResponse> IProxy<TManager>.RunWithRouteParams<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        Dictionary<string, object> routeParams)
    {
        return Run<TRequest, TResponse>(getFunc, requestStream: null, requestDto: default, routeParams: routeParams);
    }

    Task<TResponse> IProxy<TManager>.RunWithRequestStreamAndRouteParams<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        System.IO.Stream requestBody,
        Dictionary<string, object> routeParams)
    {
        return Run<TRequest, TResponse>(getFunc, requestStream: requestBody, requestDto: default, routeParams: routeParams);
    }

    private async Task<TResponse> Run<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        System.IO.Stream? requestStream,
        TRequest? requestDto,
        Dictionary<string, object>? routeParams)
        where TRequest : CLI.RequestBase
        where TResponse : CLI.ResponseBase
    {
        try
        {
            // A context needs to be built through middleware or some other means in the 
            // client before making a call through the proxy.
            if (!_contextFactoryUtility.TryGetContext(out _))
            {
                _logger.LogError("A context must be built before calling the proxy.");

                return CreateErrorResponse<TResponse>(new InternalError());
            }

            TRequest? dto;

            // If a stream is passed in, convert from the stream to a JSON with any
            // route parameters if they're available.
            if (requestStream is not null)
            {
                dto = await _converter.Convert<TRequest>(requestStream, routeParams);


                if (dto is null)
                {
                    return CreateErrorResponse<TResponse>(new ValidationError
                    {
                        Errors = new Dictionary<string, string[]>
                        {
                            { "Request Body", new[] { "Request body was not properly formatted." } }
                        }
                    });
                }
            }
            // If a DTO Is passed in, just use the DTO. Assumption is any parameters coming
            // from a route will already be part of the DTO.
            else if (requestDto is not null)
            {
                dto = requestDto;
            }
            // Otherwise, construct the DTO from only route parameters.
            else
            {
                dto = (await _converter.Convert<TRequest>(default, routeParams))!;
            }

            var validationError = _validator.Validate(dto);

            if (validationError is not null)
            {
                return CreateErrorResponse<TResponse>(validationError);
            }

            var func = getFunc(_manager);
            var authorizationError = await _authorizer.Authorize(func.Method, dto);

            if (authorizationError is not null)
            {
                return CreateErrorResponse<TResponse>(authorizationError);
            }

            return await func(dto);
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Resource not found.");

            return CreateErrorResponse<TResponse>(new NotFoundError { PublicMessage = ex.PublicMessage });
        }
        catch (ConflictException ex)
        {
            return CreateErrorResponse<TResponse>(new ConflictError { PublicMessage = ex.PublicMessage });
        }
        catch (ForbiddenException ex)
        {
            return CreateErrorResponse<TResponse>(new ForbiddenError { PublicMessage = ex.PublicMessage });
        }
        catch (ExternalTimeoutException ex)
        {
            _logger.LogError(ex, "External timeout error caught in proxy.");

            return CreateErrorResponse<TResponse>(new ExternalTimeoutError());
        }
        catch (Nile.Common.Exceptions.ValidationException ex)
        {
            _logger.LogError(ex, "Validation error caught in proxy.");

            return CreateErrorResponse<TResponse>(new ValidationError
            {
                Errors = ex.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error caught in proxy.");

            return CreateErrorResponse<TResponse>(ex);
        }
    }

    private static TResponse CreateErrorResponse<TResponse>(ErrorBase error) where TResponse : CLI.ResponseBase
    {
        var response = (TResponse)Activator.CreateInstance(typeof(TResponse))!;
        response.Error = error;

        return response;
    }

    private static TResponse CreateErrorResponse<TResponse>(Exception ex) where TResponse : CLI.ResponseBase
    {
        var response = ex switch
        {
            _ => CreateErrorResponse<TResponse>(new InternalError())
        };

        return response;
    }
}
