FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching on restore
COPY src/Heum.Contracts/Heum.Contracts.csproj            src/Heum.Contracts/
COPY src/Heum.Data/Heum.Data.csproj                      src/Heum.Data/
COPY src/Heum.Infrastructure/Heum.Infrastructure.csproj  src/Heum.Infrastructure/
COPY src/Heum.ServiceDefaults/Heum.ServiceDefaults.csproj src/Heum.ServiceDefaults/
COPY src/Heum.Server/Heum.Server.csproj                  src/Heum.Server/

RUN dotnet restore src/Heum.Server/Heum.Server.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/Heum.Server/Heum.Server.csproj \
    -c Release -o /app/publish --no-restore --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Required environment variables (set via orchestrator / docker-compose / Aspire):
#   ConnectionStrings__heumdb    — PostgreSQL connection string
#   ConnectionStrings__cache     — Redis connection string
#   ConnectionStrings__messaging — Azure Service Bus connection string
#   Keycloak__*                  — Keycloak admin API settings

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Heum.Server.dll"]
