using Microsoft.Extensions.Logging;
using Nile.Common;
using Nile.Common.InternalDTOs;

namespace Nile.Utilities.AzureSdk;

public abstract class UtilityBase : ServiceContractBase
{
    protected UtilityBase(ILogger logger) : base(logger)
    {
    }
}