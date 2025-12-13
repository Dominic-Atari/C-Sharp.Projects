using System.Reflection;
using Nile.Common.Errors;
using Nile.Utilities;

namespace Nile.Managers.Proxys;

public class ProxyAuthorizer : IAuthorizer
{
    private readonly ISecurityUtility _securityUtility;
    
    public ProxyAuthorizer(ISecurityUtility securityUtility)
    {
        _securityUtility = securityUtility;
    }

    public async Task<ErrorBase?> Authorize(MethodInfo method, CLI.RequestBase request)
    {
        var isAuthorized = await _securityUtility.IsAuthorized(request);
        
        return isAuthorized ? null : new ForbiddenError();
    }
}