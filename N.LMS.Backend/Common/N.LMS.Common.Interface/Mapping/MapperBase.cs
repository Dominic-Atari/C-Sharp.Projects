using AutoMapper;

namespace N.LMS.Common.Interface.Mapping;

public abstract class MapperBase
{
    private readonly IMapper _mapper;

    protected MapperBase(MapperConfiguration configuration)
    {
        _mapper = configuration.CreateMapper();
    }

    public TDestination Map<TDestination>(object source) => _mapper.Map<TDestination>(source);

    public TDestination Map<TSource, TDestination>(TSource source) => _mapper.Map<TSource, TDestination>(source);

    protected static MapperConfiguration CreateConfiguration(Action<IMapperConfigurationExpression> configure) =>
        new(configure);
}
