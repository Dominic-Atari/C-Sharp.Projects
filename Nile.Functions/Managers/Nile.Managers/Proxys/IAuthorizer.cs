using System.Reflection;
using Nile.Common.Errors;
using Nile.Common.InternalDTOs;

namespace Nile.Managers.Proxys;

public interface IAuthorizer
{
    Task <ErrorBase?> Authorize(MethodInfo method, CLI.RequestBase request);
}