using Microsoft.EntityFrameworkCore;
using N.LMS.Database.Interface.Model;

namespace N.LMS.Database.Factory;

public static class DatabaseFactory
{
    public static NileDbContext CreateContext()
    {
        var connectionString = new ConfigurationUtility().NileDbConnectionString;
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        var options = new DbContextOptionsBuilder<NileDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;
        return new NileDbContext(options);
    }
}
