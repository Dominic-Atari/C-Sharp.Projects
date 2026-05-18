using System.Collections.Concurrent;
using System.Threading;
using Nile.Common.Contexts;

namespace Nile.Utilities;

/// <summary>
/// Minimal ambient context provider used by proxies and managers. Stores a ContextBase per async flow.
/// </summary>
public sealed class ContextFactoryUtility : IContextFactoryUtility
{
    private static readonly AsyncLocal<ContextBase?> _current = new();

    public void BuildContext(Type type, string? authHeaderValue = null)
    {
        if (!typeof(ContextBase).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type.FullName} must derive from ContextBase", nameof(type));
        }

        // Instantiate using the default constructor; attach auth header when available via a known property if present
        var context = (ContextBase?)Activator.CreateInstance(type) ?? throw new InvalidOperationException("Context instantiation failed");

        // Optionally set Authorization header if the context exposes a property named "Authorization" or "AuthHeader"
        var authProp = type.GetProperty("Authorization") ?? type.GetProperty("AuthHeader");
        if (authProp != null && authProp.CanWrite)
        {
            authProp.SetValue(context, authHeaderValue);
        }

        // If the context exposes a UserId property, attempt to hydrate it from the auth header (supports "Bearer <guid>" or raw guid)
        var userIdProp = type.GetProperty("UserId");
        if (userIdProp != null && userIdProp.CanWrite && authHeaderValue is { Length: > 0 })
        {
            var candidate = authHeaderValue.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (Guid.TryParse(candidate, out var userId))
            {
                userIdProp.SetValue(context, userId);
            }
        }

        _current.Value = context;
    }

    public T GetContext<T>() where T : ContextBase
    {
        if (_current.Value is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException("Context has not been built for the current execution.");
    }

    public bool TryGetContext(out ContextBase? context)
    {
        context = _current.Value;
        return context is not null;
    }
}
