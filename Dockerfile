FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build context is the repository root. Locate W1's API project rather than
# hard-coding a layout that may change while the API is being implemented.
COPY . .

RUN set -eux; \
    project="$(find /src -type f -name 'BdCommerce.Api.csproj' -print -quit)"; \
    test -n "$project"; \
    dotnet restore "$project"; \
    dotnet publish "$project" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# curl is used by the container healthcheck below.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && apt-get clean \
 && groupadd --system appuser \
 && useradd --system --gid appuser --create-home appuser \
 && mkdir -p /app \
 && chown -R appuser:appuser /app

WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish/ /app/

USER appuser
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "BdCommerce.Api.dll"]
