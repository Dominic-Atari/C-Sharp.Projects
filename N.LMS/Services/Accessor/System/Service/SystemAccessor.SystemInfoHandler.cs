using System.Reflection;
using N.LMS.Accessor.System.Interface.Model;
using N.LMS.Accessor.System.Interface.Request;
using N.LMS.Accessor.System.Interface.Result;

namespace N.LMS.Accessor.System.Service;

internal sealed partial class SystemAccessor
{
    private Task<SystemInfoResult> Handle(SystemInfoRequest _)
    {
        var entry = Assembly.GetEntryAssembly() ?? typeof(SystemAccessor).Assembly;
        var version = entry.GetName().Version?.ToString() ?? "0.0.0";
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                 ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                 ?? "Production";

        return Task.FromResult(new SystemInfoResult
        {
            Info = new SystemInfo
            {
                Version = version,
                Environment = env,
                AssemblyName = entry.GetName().Name ?? string.Empty,
                ServerTimeUtc = DateTime.UtcNow
            }
        });
    }
}
