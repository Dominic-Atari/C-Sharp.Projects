# Docker Setup for Nile Functions

## Prerequisites
- Docker Desktop installed and running
- .NET 8 SDK installed

## SQL Server Setup

### 1. Start SQL Server Container
```powershell
docker-compose up -d
```

### 2. Verify Container is Running
```powershell
docker ps
```

### 3. Check SQL Server Logs
```powershell
docker logs nile-sqlserver
```

### 4. Connect to SQL Server
**Connection String for Local Development:**
```
Server=localhost,1433;Database=NileDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;
```

**Using SQL Server Management Studio (SSMS):**
- Server: `localhost,1433`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: `YourStrong@Passw0rd`

**Using Azure Data Studio:**
- Connection type: Microsoft SQL Server
- Server: `localhost,1433`
- Authentication type: SQL Login
- User name: `sa`
- Password: `YourStrong@Passw0rd`

### 5. Run Database Migrations
```powershell
cd Data\Nile.DbUp
dotnet run
```

## Configuration Files

### local.settings.json (Functions)
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SqlServerConnectionString": "Server=localhost,1433;Database=NileDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;"
  }
}
```

### local.settings.json (DbUp)
```json
{
  "IsEncrypted": false,
  "Values": {
    "SqlServerConnectionString": "Server=localhost,1433;Database=NileDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;"
  }
}
```

## Docker Commands

### Stop SQL Server
```powershell
docker-compose down
```

### Stop and Remove Data (Fresh Start)
```powershell
docker-compose down -v
```

### View Container Status
```powershell
docker-compose ps
```

### Access SQL Server CLI
```powershell
docker exec -it nile-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd
```

## Troubleshooting

### Container Won't Start
1. Check if port 1433 is already in use:
```powershell
netstat -ano | findstr :1433
```

2. Check Docker logs:
```powershell
docker logs nile-sqlserver
```

### Connection Refused
- Wait 10-15 seconds after starting the container for SQL Server to initialize
- Verify container health: `docker inspect nile-sqlserver`

### Database Not Created
- Run DbUp migrations: `cd Data\Nile.DbUp; dotnet run`
- DbUp will automatically create the database if it doesn't exist

## Security Notes
**IMPORTANT**: 
- The password `YourStrong@Passw0rd` is for LOCAL DEVELOPMENT ONLY
- Never commit actual passwords to source control
- Use Azure Key Vault or User Secrets for production credentials
- Change the SA password after first login in production environments

## Data Persistence
- Database data is stored in a Docker volume named `sqlserver-data`
- Data persists across container restarts
- To delete all data: `docker-compose down -v`
