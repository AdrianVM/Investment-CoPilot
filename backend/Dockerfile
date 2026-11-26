# syntax=docker/dockerfile:1
# API image: built with the .NET SDK, run on the ASP.NET Core runtime. Configured only through environment
# variables (for example ConnectionStrings__Database, Auth__Authority); it contains no settings or credentials.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files only, so this layer is reused until dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY --parents src/**/*.csproj ./
RUN dotnet restore src/Host/Host.csproj

COPY src/ src/
# OpenAPI document generation starts the host at build time; the image does not need it.
RUN dotnet publish src/Host/Host.csproj -c Release --no-restore -o /app -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
# The runtime image's non-root user; the app listens on port 8080 (ASPNETCORE_HTTP_PORTS).
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "InvestmentCoPilot.Host.dll"]
