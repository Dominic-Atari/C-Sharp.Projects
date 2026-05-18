using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nile.Managers.Contract.Client.Mapping;

public static class ClientDtoMapper
{
    private static IMapper? _mapper;
    private static MapperConfiguration? _config;

    private static IMapper Mapper => _mapper ??= Configuration.CreateMapper();

    public static MapperConfiguration Configuration
    {
        get
        {
            if (_config == null)
            {
                _config = new MapperConfiguration(
                    cfg => { cfg.AddProfile<ClientContractsProfile>(); },
                    NullLoggerFactory.Instance);
            }

            return _config;
        }
    }

    public static TDestination Map<TDestination>(object source)
    {
        return Mapper.Map<TDestination>(source);
    }

    public static void Map(object source, object destination)
    {
        Mapper.Map(source, destination, source.GetType(), destination.GetType());
    }
}
