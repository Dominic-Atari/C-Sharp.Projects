using System.Threading.Tasks;
using CLI = Nile.Managers.Contract.Client.DataContract;

namespace Nile.Utilities
{
    // No-op implementation that always authorizes requests in local/dev
    public sealed class NoOpSecurityUtility : ISecurityUtility
    {
        public Task<bool> IsAuthorized(CLI.RequestBase request)
            => Task.FromResult(true);
    }
}
