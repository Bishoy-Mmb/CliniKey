# CliniKey — Project Silver Repo Dockerfile
# .NET 10 + PostgreSQL | Schema-per-tenant multi-tenancy
# Rules: pinned base tag, pinned packages, --no-install-recommends on every apt-get

FROM mcr.microsoft.com/dotnet/sdk:10.0.100

WORKDIR /app

# Install system dependencies — all pinned, --no-install-recommends required
RUN apt-get update && apt-get install -y --no-install-recommends \
    postgresql-client=16+257build1.1 \
    curl=8.5.0-2ubuntu10.6 \
    git=1:2.43.0-1ubuntu7.3 \
    && rm -rf /var/lib/apt/lists/*

# Copy solution and restore dependencies first (layer caching)
COPY CliniKey.slnx ./
COPY src/ ./src/
COPY tests/ ./tests/

# Restore NuGet packages
RUN dotnet restore CliniKey.slnx

# Build the solution (0 warnings enforced)
RUN dotnet build CliniKey.slnx --no-restore --configuration Release

# Default: run unit tests only (no Docker required)
CMD ["dotnet", "test", "CliniKey.slnx", "--no-build", "--configuration", "Release", "--filter", "Category!=Integration"]
