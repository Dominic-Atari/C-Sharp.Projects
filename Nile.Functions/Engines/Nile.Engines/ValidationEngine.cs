using Microsoft.Extensions.Logging;
using Nile.Common;

namespace Nile.Engines{

    public class EngineBase : ServiceContainerBase
    {
        protected EngineBase(ILogger logger) : base(logger)
        {
        }
    }
}