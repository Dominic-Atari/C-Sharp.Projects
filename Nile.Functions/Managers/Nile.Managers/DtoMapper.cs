using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nile.Managers;

// NOTE: This mapper is intentionally limited to INTERNAL mappings only.
// It must not depend on Client Contracts to avoid circular references.
internal static class DtoMapper
{
    private static IMapper? _mapper;
    private static MapperConfiguration? _configuration;

    private static IMapper Mapper => _mapper ??= Configuration.CreateMapper();

    public static MapperConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new MapperConfiguration(
                    cfg =>
                    {
                        // Intentionally empty: add internal (Common/Engines/Accessors) maps here when needed.
                    },
                    NullLoggerFactory.Instance);
            }

            return _configuration;
        }
    }

    public static void Map(object source, object destination)
    {
        Mapper.Map(source, destination, source.GetType(), destination.GetType());
    }

    public static TDestination Map<TDestination>(object source)
    {
        return Mapper.Map<TDestination>(source);
    }
}