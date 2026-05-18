using Microsoft.Extensions.Logging;

namespace Nile.Common;

public abstract class ServiceContainerBase
{
    protected ILogger Logger { get; set; }
    
    public ServiceContainerBase(ILogger logger)
    {
        Logger = logger;
    }
    
    public virtual string TestMe(string input)
    {
        string result = $"{input} : {GetType().Name}";
        Console.WriteLine(result);
        return result;
    }
}