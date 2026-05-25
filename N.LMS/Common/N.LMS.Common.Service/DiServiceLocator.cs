using Microsoft.Extensions.DependencyInjection;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Service;

internal sealed class DiServiceLocator : IServiceLocator
{
    private readonly IServiceProvider _provider;

    public DiServiceLocator(IServiceProvider provider) => _provider = provider;

    public T Resolve<T>() where T : IComponent => _provider.GetRequiredService<T>();
}
