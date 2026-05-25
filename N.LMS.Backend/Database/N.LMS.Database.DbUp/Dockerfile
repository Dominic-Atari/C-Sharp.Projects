# syntax=docker/dockerfile:1
# Build context: the N.LMS.Backend/ folder.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish Database/N.LMS.Database.DbUp/N.LMS.Database.DbUp.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "N.LMS.Database.DbUp.dll"]
