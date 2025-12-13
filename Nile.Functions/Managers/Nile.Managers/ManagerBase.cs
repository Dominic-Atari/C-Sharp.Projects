using Microsoft.Extensions.Logging;
using Nile.Common;

namespace Nile.Managers;

public class ManagerBase : ServiceContainerBase
{
    public ManagerBase(ILogger logger) : base(logger)
    {
    }
}