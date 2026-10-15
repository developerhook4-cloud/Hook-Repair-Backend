# Cambia 10.0 por la version de <TargetFramework> de HookRepair.Api.csproj
# (net8.0 -> 8.0, net9.0 -> 9.0, net10.0 -> 10.0)
ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY . .
RUN dotnet restore HookRepair.Api/HookRepair.Api.csproj
RUN dotnet publish HookRepair.Api/HookRepair.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Railway asigna el puerto en la variable PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet HookRepair.Api.dll"]
