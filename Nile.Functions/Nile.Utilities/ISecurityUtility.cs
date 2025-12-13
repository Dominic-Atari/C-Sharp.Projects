using CLI = Nile.Managers.Contract.Client.DataContract;

namespace Nile.Utilities;

public interface ISecurityUtility
{
    Task<bool> IsAuthorized(CLI.RequestBase request);
}