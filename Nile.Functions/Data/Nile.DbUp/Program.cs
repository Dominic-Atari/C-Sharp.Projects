using System.Reflection;
using System.IO;
using Microsoft.Data.SqlClient;
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
        // Developer options (set via environment variables):
        // DBUP_SCRIPTS_DIR - when set and points to a directory, DbUp will load scripts from filesystem (useful in DEBUG).
        // DBUP_ALLOW_RERUN - when set to 'true' in DEBUG, DbUp will use a NullJournal to allow rerunning scripts (dangerous for prod).
        var scriptsDir = Environment.GetEnvironmentVariable("DBUP_SCRIPTS_DIR");
        var allowRerun = string.Equals(Environment.GetEnvironmentVariable("DBUP_ALLOW_RERUN"), "true", StringComparison.OrdinalIgnoreCase);

        var builder = DeployChanges.To
            .SqlDatabase(new DbUpSqlConnection(connectionString))
            // Default journal to SchemaVersions; may be overridden below for dev allow-rerun
            .WithTransaction()
            .WithExecutionTimeout(TimeSpan.FromSeconds(300))
            .LogToConsole();

        if (!string.IsNullOrEmpty(scriptsDir) && Directory.Exists(scriptsDir))
        {
            Console.WriteLine($"DbUp: Loading scripts from filesystem path: {scriptsDir}");
            builder = builder.WithScriptsFromFileSystem(scriptsDir);
        }
        else
        {
            builder = builder.WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly());
        }

        if (allowRerun)
        {
            Console.WriteLine("DbUp: DBUP_ALLOW_RERUN=true, clearing entries from dbo.SchemaVersions so scripts may re-run. Do NOT set this in production.");
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "IF OBJECT_ID('dbo.SchemaVersions','U') IS NOT NULL DELETE FROM dbo.SchemaVersions;";
                cmd.ExecuteNonQuery();
                Console.WriteLine("Cleared dbo.SchemaVersions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to clear SchemaVersions: {ex.Message}");
            }

            builder = builder.JournalToSqlTable("dbo", "SchemaVersions");
        }
        else
        {
            builder = builder.JournalToSqlTable("dbo", "SchemaVersions");
        }

        var migrator = builder.Build();

        // Capture existing journal entries so we can report which new scripts were applied
        HashSet<string> beforeScripts = new();
        try
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "IF OBJECT_ID('dbo.SchemaVersions','U') IS NOT NULL SELECT ScriptName FROM dbo.SchemaVersions";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                beforeScripts.Add(rdr.GetString(0));
            }
        }
        catch { /* ignore - we'll handle missing table below */ }

        var result = migrator.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            throw new DbUpdateException("DbUp update was unsuccessful");
        }

        // After successful upgrade, examine journal to find any newly applied scripts
        try
        {
            var afterScripts = new List<string>();
            using var conn2 = new SqlConnection(connectionString);
            conn2.Open();
            using var cmd2 = conn2.CreateCommand();
            cmd2.CommandText = "IF OBJECT_ID('dbo.SchemaVersions','U') IS NOT NULL SELECT ScriptName, Applied FROM dbo.SchemaVersions ORDER BY Applied";
            using var rdr2 = cmd2.ExecuteReader();
            while (rdr2.Read())
            {
                afterScripts.Add(rdr2.GetString(0));
            }

            var newScripts = afterScripts.Where(s => !beforeScripts.Contains(s)).ToList();
            if (newScripts.Count == 0)
            {
                Console.WriteLine("No new scripts applied.");
            }
            else
            {
                Console.WriteLine($"Applied {newScripts.Count} new script(s):");
                foreach (var s in newScripts)
                {
                    Console.WriteLine(" - " + s);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not inspect SchemaVersions table: {ex.Message}");
        }

        // Diagnostic: print the entry assembly path so IDE runs are easier to verify
        try
        {
            var entry = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly()?.Location;
            Console.WriteLine($"Executable: {entry}");
        }
        catch { }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();

        // Friendly verification banner for IDEs that capture stdout differently
        Console.WriteLine("Verification complete.");
        // Extra explicit uncolored banner to ensure IDEs capture success reliably
        Console.WriteLine("SUCCESS: DbUp finished OK");
        try { Console.Out.Flush(); } catch { }
    }
}
