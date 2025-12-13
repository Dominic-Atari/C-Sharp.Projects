using Microsoft.Extensions.Logging;
using AutoMapper;
namespace Nile.Accessors;

internal interface IMapper
{
    IConfigurationProvider Configuration { get; }
    void Map(object source, object destination);
    T Map<T>(object source);
}