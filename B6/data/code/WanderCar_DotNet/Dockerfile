# syntax=docker/dockerfile:1

# =====================================================================
#  WanderCar — .NET backend (ASP.NET Core 8.0)
#  Build context: ./WanderCar_DotNet
# =====================================================================

# ---------------------------------------------------------------------
# Stage 1 — restore + publish
# net8.0 to match <TargetFramework>net8.0</TargetFramework> in FlemanApi.csproj.
# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build

WORKDIR /build

# csproj first so the NuGet restore layer is reused whenever only code changes.
# FlemanApi.Tests is deliberately left out — it isn't part of the runtime image.
COPY src/FlemanApi/FlemanApi.csproj src/FlemanApi/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/FlemanApi/FlemanApi.csproj

COPY src/FlemanApi/ src/FlemanApi/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/FlemanApi/FlemanApi.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

# ---------------------------------------------------------------------
# Stage 2 — runtime
# Same Ubuntu Jammy base family as the Java image so both services share
# one OS layer lineage, one package manager, and one healthcheck tool.
# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

LABEL org.opencontainers.image.title="wandercar-dotnet-backend" \
      org.opencontainers.image.description="WanderCar fleet management backend (ASP.NET Core)" \
      org.opencontainers.image.version="1.0.0"

# curl backs the healthcheck below. 'app' is the non-root user already
# present in the .NET 8 runtime images — no need to create one here.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish ./

# logs/ is written by the Serilog file sink configured in Program.cs.
RUN mkdir -p /app/logs && chown -R app:app /app

USER app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Kolkata

# Same probe as the Java image: DataSeeder.SeedAsync runs before app.Run(),
# so a non-empty /api/states means the fixture data is committed to MySQL.
#
# start-interval probes every 5s while booting so compose's depends_on gate
# releases promptly; once healthy it drops to 5m. The app logs every request,
# so a short steady-state interval floods `docker compose logs` forever.
HEALTHCHECK --interval=5m --timeout=5s --start-period=120s --start-interval=5s --retries=3 \
  CMD curl -fsS http://localhost:8080/api/states | grep -q Maharashtra || exit 1

ENTRYPOINT ["dotnet", "FlemanApi.dll"]
