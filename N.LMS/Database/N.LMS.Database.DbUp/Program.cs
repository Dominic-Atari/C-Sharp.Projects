using System.Reflection;
using DbUp;
using N.LMS.Database.Factory;

var connectionString = new ConfigurationUtility().NileDbConnectionString;

EnsureDatabase.For.MySqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .MySqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return -1;
}

Console.WriteLine("DbUp migration complete.");
return 0;
