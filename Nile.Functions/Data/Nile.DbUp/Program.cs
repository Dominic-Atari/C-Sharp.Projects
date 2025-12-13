using System.Reflection;
using DbUp;
using Microsoft.EntityFrameworkCore;
using Nile.Common;
using Nile.Common.Extensions;
using Nile.Database;
using Nile.Database.DataContracts;

namespace Nile.DbUp;

internal static class Program
{
    internal static readonly IConfigUtility ConfigUtility = new ConfigUtility(ConfigBuilder.Build());

    static void Main()
    {
        // DbUp runs against the local and test db if you're in DEBUG
        var connectionStrings = new List<string> { ConfigUtility.SqlServerConnectionString };
        
#if DEBUG
        // Add test connection string only if it's configured
        if (!string.IsNullOrEmpty(ConfigUtility.SqlServerTestConnectionString))
        {
            connectionStrings.Add(ConfigUtility.SqlServerTestConnectionString);
        }
#endif

        foreach (var connectionString in connectionStrings)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string environment variable missing.");
            }

            Console.WriteLine($"Running UpdateDb for connectionString: '{connectionString}'");

            if (!DatabaseContext.ShouldUseAzureAccessTokenAuth(ConfigUtility))
            {
                // SQL database will already be there on Azure via ARM template
                EnsureDatabase.For.SqlDatabase(connectionString);
            }

            UpdateDb(connectionString);
        }
    }

    private static void UpdateDb(string connectionString)
    {
        var migrator = DeployChanges.To
            .SqlDatabase(new DbUpSqlConnection(connectionString))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .WithTransaction()
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithExecutionTimeout(TimeSpan.FromSeconds(300))
            .LogToConsole()
            .Build();

        var result = migrator.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            throw new DbUpdateException("DbUp update was unsuccessful");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
    }
}
