using Microsoft.Extensions.Logging;
namespace Nile.Common.InternalDTOs
{
    public abstract class ServiceContractBase
    {
       protected ILogger Logger { get; init; }

         protected ServiceContractBase(ILogger logger)
         {
              Logger = logger;
         }
    }
}