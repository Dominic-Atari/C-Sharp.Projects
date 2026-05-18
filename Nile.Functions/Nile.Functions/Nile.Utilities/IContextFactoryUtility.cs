using Nile.Common.Contexts;

namespace Nile.Utilities;

public interface IContextFactoryUtility
{
    void BuildContext(Type type, string? authHeaderValue = null);

    T GetContext<T>() where T : ContextBase;

    bool TryGetContext(out ContextBase? context);
}