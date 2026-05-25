namespace N.LMS.Common.Interface.Framework;

/// <summary>
/// Base for leaf services (utilities) that do not need ProxyForService&lt;T&gt;().
/// Use <see cref="ProxyEnabledServiceBase"/> for accessors / engines / managers.
/// </summary>
public abstract class ServiceBase : IComponent
{
}
