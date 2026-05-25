namespace N.LMS.Common.Interface.Framework;

public abstract class ProxyEnabledServiceBase : IComponent
{
    protected T ProxyForService<T>() where T : IComponent =>
        ServiceLocator.Current.Resolve<T>();
}
