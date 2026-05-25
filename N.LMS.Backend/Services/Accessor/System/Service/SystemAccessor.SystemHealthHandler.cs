using N.LMS.Accessor.System.Interface.Model;
using N.LMS.Accessor.System.Interface.Request;
using N.LMS.Accessor.System.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.System.Service;

internal sealed partial class SystemAccessor
{
    private async Task<SystemHealthResult> Handle(SystemHealthRequest _)
    {
        var now = DateTime.UtcNow;
        bool reachable;
        string? message = null;
        try
        {
            await using var db = DatabaseFactory.CreateContext();
            reachable = await db.Database.CanConnectAsync();
            if (!reachable) message = "Database connection check returned false.";
        }
        catch (Exception ex)
        {
            reachable = false;
            message = ex.Message;
        }

        return new SystemHealthResult
        {
            Health = new SystemHealth
            {
                Healthy = reachable,
                DatabaseReachable = reachable,
                Message = message,
                CheckedAtUtc = now
            }
        };
    }
}
