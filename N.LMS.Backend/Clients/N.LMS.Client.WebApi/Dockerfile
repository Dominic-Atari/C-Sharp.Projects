# syntax=docker/dockerfile:1
# Build context: the N.LMS.Backend/ folder (so ProjectReferences across the solution resolve).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the whole tree; .dockerignore excludes bin/obj so this stays cheap.
COPY . .

RUN dotnet publish Clients/N.LMS.Client.WebApi/N.LMS.Client.WebApi.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "N.LMS.Client.WebApi.dll"]
