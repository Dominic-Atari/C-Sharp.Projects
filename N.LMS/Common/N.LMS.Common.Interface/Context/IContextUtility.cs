using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Interface.Context;

public interface IContextUtility : IUtility
{
    RequestContext GetContext();
    void SetContext(RequestContext context);

    Task<TContext> GetRequiredContext<TContext>() where TContext : class;
    void SetTypedContext<TContext>(TContext context) where TContext : class;

    /// <summary>
    /// Read the raw source context deposited by the caller (e.g. WebContext) and derive
    /// any ambient contexts the call graph will need (e.g. UserContext). Called by
    /// ContextBuildingInterceptor at manager entry; safe to call repeatedly.
    /// </summary>
    Task BuildAmbientContext();
}
