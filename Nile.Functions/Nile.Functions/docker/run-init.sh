#!/bin/bash
set -euo pipefail

echo "Starting SQL Server..."
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to accept connections
echo "Waiting for SQL Server to be ready..."
until /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; do
  sleep 1
done
echo "SQL Server is ready."

# Create NileDb if missing
echo "Ensuring database NileDb exists..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "IF DB_ID('NileDb') IS NULL CREATE DATABASE [NileDb];"

# If the scripts were mounted into /scripts, execute the main recreate script
if [ -f /scripts/000.RecreateAllTables.sql ]; then
  echo "Running /scripts/000.RecreateAllTables.sql..."
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d NileDb -i /scripts/000.RecreateAllTables.sql
  echo "Recreate script finished."
else
  echo "No /scripts/000.RecreateAllTables.sql found; skipping recreate step."
fi

tail -f /var/opt/mssql/log/errorlog
