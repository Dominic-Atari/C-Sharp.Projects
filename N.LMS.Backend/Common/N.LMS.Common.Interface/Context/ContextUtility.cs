using System.Security.Claims;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Interface.Context;

public sealed class ContextUtility : ServiceBase, IContextUtility
{
    private static readonly AsyncLocal<RequestContext?> _request = new();
    private static readonly AsyncLocal<Dictionary<Type, object>?> _typed = new();

    public RequestContext GetContext() => _request.Value ?? new RequestContext();

    public void SetContext(RequestContext context) => _request.Value = context;

    public Task<TContext> GetRequiredContext<TContext>() where TContext : class
    {
        var bag = _typed.Value;
        if (bag is not null && bag.TryGetValue(typeof(TContext), out var value) && value is TContext typed)
        {
            return Task.FromResult(typed);
        }
        throw new InvalidOperationException(
            $"No ambient context of type '{typeof(TContext).Name}' has been set for this request.");
    }

    public void SetTypedContext<TContext>(TContext context) where TContext : class
    {
        var bag = _typed.Value ??= new Dictionary<Type, object>();
        bag[typeof(TContext)] = context;
    }

    public Task BuildAmbientContext()
    {
        var bag = _typed.Value;
        if (bag is null) return Task.CompletedTask;

        // WebContext → UserContext (claims → identity). Idempotent.
        if (bag.TryGetValue(typeof(WebContext), out var raw) && raw is WebContext web
            && !bag.ContainsKey(typeof(UserContext)))
        {
            bag[typeof(UserContext)] = DeriveUserContext(web);
        }

        return Task.CompletedTask;
    }

    private static UserContext DeriveUserContext(WebContext web)
    {
        var idValue = web.FindClaim("sub") ?? web.FindClaim(ClaimTypes.NameIdentifier);
        Guid.TryParse(idValue, out var userId);

        return new UserContext
        {
            UserId = userId,
            Email = web.FindClaim(ClaimTypes.Email) ?? string.Empty,
            DisplayName = web.FindClaim(ClaimTypes.Name) ?? string.Empty,
            Roles = web.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray()
        };
    }
}
