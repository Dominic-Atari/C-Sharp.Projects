using System.Collections.Generic;
using System.Data;
using DbUp.Engine.Transactions;
using DbUp.Support;
using Microsoft.Data.SqlClient;

namespace Nile.DbUp;

/// <summary>
/// Custom SQL connection for DbUp migrations with Azure AD authentication support.
/// </summary>
public class DbUpSqlConnection : DatabaseConnectionManager
{
    private readonly string _connectionString;

    public DbUpSqlConnection(string connectionString)
        : base(l => CreateConnection(connectionString))
    {
        _connectionString = connectionString;
    }

    private static IDbConnection CreateConnection(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        return connection;
    }

    public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
    {
        var commandSplitter = new SqlCommandSplitter();
        var scriptStatements = commandSplitter.SplitScriptIntoCommands(scriptContents);
        return scriptStatements;
    }
}
