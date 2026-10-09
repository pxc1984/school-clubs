# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Layer 1: only props and project files (restore cache survives source changes)
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Web/Web.csproj src/Web/

# Без --mount=type=cache: пакеты остаются в слое и попадают в кэш сборки CI (GHA/registry). С cache mount
# слой restore брался из кэша, а сам mount на свежем раннере пуст — publish --no-restore не находил пакеты.
RUN dotnet restore src/Web/Web.csproj

# Layer 2: source
COPY src/ src/

# Исходники остаются в стадии build — в итоговый образ попадает только /app/publish.
# DebugType=none: не кладём .pdb (в них пути к исходникам), UseAppHost=false: запуск через `dotnet`.
WORKDIR /src/src/Web
RUN dotnet publish Web.csproj -c Release -o /app/publish --no-restore \
    -p:DebugType=none -p:DebugSymbols=false -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "SchoolClubs.Web.dll"]
