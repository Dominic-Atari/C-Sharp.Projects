namespace Nile.Managers.Proxys;

public interface IConverter
{
    Task<TRequest?> Convert<TRequest>(System.IO.Stream? requestBody, Dictionary<string, object>? routeParams) where TRequest : CLI.RequestBase;
}