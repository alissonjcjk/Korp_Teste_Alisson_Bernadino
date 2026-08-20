# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar apenas o .csproj para aproveitar cache de camadas do Docker
COPY src/BillingService.Api/BillingService.Api.csproj ./src/BillingService.Api/
RUN dotnet restore ./src/BillingService.Api/BillingService.Api.csproj

# Copiar o restante do código-fonte
COPY src/ ./src/

# Publicar em modo Release
RUN dotnet publish ./src/BillingService.Api/BillingService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Instalar curl para healthcheck (executado como root antes de trocar usuário)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Usuário não-root por segurança
RUN adduser --disabled-password --gecos "" --no-create-home appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "BillingService.Api.dll"]
