

namespace Nile.Managers.Proxys;

public interface IProxy<out TManager> where TManager : notnull
{
    Task<TResponse> RunWithoutRequestBody<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> grtFunc)
        where TRequest : CLI.RequestBase
        where TResponse : CLI.ResponseBase;
    
    Task<TResponse> RunWithRequestDto<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        TRequest request)
        where TRequest : CLI.RequestBase
        where TResponse : CLI.ResponseBase;

    Task<TResponse> RunWithRequestStream<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        System.IO.Stream requestBody) where TResponse : CLI.ResponseBase where TRequest : CLI.RequestBase;

    Task<TResponse> RunWithRouteParams<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        Dictionary<string, object> routeParams)
        where TRequest : CLI.RequestBase
        where TResponse : CLI.ResponseBase;

    Task<TResponse> RunWithRequestStreamAndRouteParams<TRequest, TResponse>(
        Func<TManager, Func<TRequest, Task<TResponse>>> getFunc,
        System.IO.Stream requestBody,
        Dictionary<string, object> routeParams)
        where TRequest : CLI.RequestBase
        where TResponse : CLI.ResponseBase;
}