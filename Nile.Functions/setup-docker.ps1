# Nile Functions - Docker SQL Server Setup Script

Write-Host "Starting Nile SQL Server Docker Setup..." -ForegroundColor Cyan

# Check if Docker is running
Write-Host "`nChecking Docker..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Start SQL Server container
Write-Host "`nStarting SQL Server container..." -ForegroundColor Yellow
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "SQL Server container started" -ForegroundColor Green
} else {
    Write-Host "Failed to start SQL Server container" -ForegroundColor Red
    exit 1
}

# Wait for SQL Server to be ready
Write-Host "`nWaiting for SQL Server to initialize (this may take 10-15 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check container health
Write-Host "`nChecking container status..." -ForegroundColor Yellow
docker ps --filter "name=nile-sqlserver" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Run database migrations
Write-Host "`nRunning database migrations..." -ForegroundColor Yellow
Push-Location Data\Nile.DbUp
try {
    dotnet run
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database migrations completed successfully" -ForegroundColor Green
    } else {
        Write-Host "Migration failed. Check the output above." -ForegroundColor Yellow
    }
} finally {
    Pop-Location
}

Write-Host "`nSetup Complete!" -ForegroundColor Green
Write-Host "`nConnection Details:" -ForegroundColor Cyan
Write-Host "  Server: localhost,1433" -ForegroundColor White
Write-Host "  Database: NileDb" -ForegroundColor White
Write-Host "  User: sa" -ForegroundColor White
Write-Host "  Password: YourStrong@Passw0rd" -ForegroundColor White

Write-Host "`nUseful Commands:" -ForegroundColor Cyan
Write-Host "  Stop SQL Server:    docker-compose down" -ForegroundColor White
Write-Host "  View logs:          docker logs nile-sqlserver" -ForegroundColor White
Write-Host "  Fresh start:        docker-compose down -v" -ForegroundColor White

Write-Host "`nRemember: This password is for LOCAL DEVELOPMENT ONLY!" -ForegroundColor Yellow
