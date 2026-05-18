
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nile.Common.Extensions;

namespace Nile.Managers.Proxys;

internal class ProxyConverter : IConverter
{
    public readonly ILogger<ProxyConverter> _logger;
    
    private readonly IConfigUtility _configUtility;
    
    public ProxyConverter(
        ILogger<ProxyConverter> logger,
        IConfigUtility configUtility)
    {
        _logger = logger;
        _configUtility = configUtility;
    }

    public async Task<TRequest?> Convert<TRequest>(
        System.IO.Stream? requestBody,
        Dictionary<string, object>? routeParams)
        where TRequest : CLI.RequestBase
    {
        TRequest? dto;

        // Body may be an empty stream or it may be null, depending on the underlying type of stream.
        // A request body is not required, but a request type to create and pass forward is required.
        if (requestBody is null)
        {
            dto = Activator.CreateInstance<TRequest>();
        }
        else
        {
            try
            {
                dto = await JsonSerializer.DeserializeAsync<TRequest>(
                    requestBody,
                    _configUtility.JsonSerializerOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize type '{TypeName}'", typeof(TRequest).Name);
                return null;
            }
        }

        if (dto is not null && routeParams is not null)
        {
            DtoMapper.Map(routeParams, dto);
        }

        return dto;
    }
}