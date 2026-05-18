using System;
using Microsoft.Data.SqlClient;

const string defaultConn = "Server=localhost,1433;Database=NileDb;User Id=sa;Password=DevP@ssw0rd!;TrustServerCertificate=true;";

var connStr = Environment.GetEnvironmentVariable("SqlServerConnectionString") ?? defaultConn;

Console.WriteLine($"Using connection string: { (connStr.Length>80 ? connStr[..80]+"..." : connStr) }");

try
{
    using var conn = new SqlConnection(connStr);
    conn.Open();

    // 1) Count NULL SchoolId
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Subjects WHERE SchoolId IS NULL;";
        var nullCount = (int)cmd.ExecuteScalar()!;
        Console.WriteLine($"Subjects with NULL SchoolId: {nullCount}");
    }

    // 2) Distribution
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"SELECT TOP 50 SchoolId, COUNT(*) AS SubjectsAssigned
FROM dbo.Subjects
GROUP BY SchoolId
ORDER BY SubjectsAssigned DESC;";
        using var rdr = cmd.ExecuteReader();
        Console.WriteLine("\nTop SchoolId distribution (SchoolId, SubjectsAssigned):");
        while (rdr.Read())
        {
            var schoolId = rdr.IsDBNull(0) ? "<NULL>" : rdr.GetGuid(0).ToString();
            var cnt = rdr.GetInt32(1);
            Console.WriteLine($"{schoolId} \t {cnt}");
        }
    }

    // 3) Recent applied scripts - be tolerant to different SchemaVersions schemas
    // Query columns separately and create new commands for each reader to avoid overlapping readers
    var cols = new System.Collections.Generic.List<string>();
    using (var cmdCols = conn.CreateCommand())
    {
        cmdCols.CommandText = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='SchemaVersions'";
        using var rdrCols = cmdCols.ExecuteReader();
        while (rdrCols.Read()) cols.Add(rdrCols.GetString(0));
    }

    if (cols.Count == 0)
    {
        Console.WriteLine("\nSchemaVersions table not found; skipping script list.");
    }
    else
    {
        Console.WriteLine("\nSchemaVersions columns: " + string.Join(", ", cols));
        string timeCol = null;
        if (cols.Contains("AppliedOn")) timeCol = "AppliedOn";
        else if (cols.Contains("ExecutedOn")) timeCol = "ExecutedOn";
        else if (cols.Contains("Applied") && cols.Contains("AppliedBy")) timeCol = "Applied"; // fallback

        if (!cols.Contains("ScriptName"))
        {
            using var cmdAll = conn.CreateCommand();
            cmdAll.CommandText = "SELECT TOP 20 * FROM dbo.SchemaVersions";
            using var rdr = cmdAll.ExecuteReader();
            Console.WriteLine("\nTop 20 SchemaVersions rows:");
            var schema = new System.Collections.Generic.List<string>();
            for (int i = 0; i < rdr.FieldCount; i++) schema.Add(rdr.GetName(i));
            Console.WriteLine(string.Join(" | ", schema));
            while (rdr.Read())
            {
                var vals = new string[rdr.FieldCount];
                for (int i = 0; i < rdr.FieldCount; i++) vals[i] = rdr.IsDBNull(i) ? "NULL" : rdr.GetValue(i).ToString();
                Console.WriteLine(string.Join(" | ", vals));
            }
        }
        else
        {
            if (timeCol != null)
            {
                using var cmdRecent = conn.CreateCommand();
                cmdRecent.CommandText = $"SELECT TOP 20 ScriptName, {timeCol} FROM dbo.SchemaVersions ORDER BY {timeCol} DESC";
                using var rdr = cmdRecent.ExecuteReader();
                Console.WriteLine($"\nRecently applied scripts (ScriptName, {timeCol}):");
                while (rdr.Read())
                {
                    var name = rdr.IsDBNull(0) ? "<null>" : rdr.GetString(0);
                    var applied = rdr.IsDBNull(1) ? "<null>" : rdr.GetValue(1).ToString();
                    Console.WriteLine($"{name} \t {applied}");
                }
            }
            else
            {
                using var cmdNames = conn.CreateCommand();
                cmdNames.CommandText = "SELECT TOP 20 ScriptName FROM dbo.SchemaVersions ORDER BY (ScriptName) DESC";
                using var rdr = cmdNames.ExecuteReader();
                Console.WriteLine("\nRecent ScriptName entries:");
                while (rdr.Read()) Console.WriteLine(rdr.IsDBNull(0) ? "<null>" : rdr.GetString(0));
            }
        }
    }

    conn.Close();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error while verifying DB: " + ex.Message);
    Environment.Exit(2);
}

Console.WriteLine("\nVerification complete.");
