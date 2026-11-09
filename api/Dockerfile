# syntax=docker/dockerfile:1
# Lalana API: build with the .NET SDK, run on the small, non-root "chiseled" ASP.NET image.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Lalana.Domain/Lalana.Domain.csproj src/Lalana.Domain/
COPY src/Lalana.Application/Lalana.Application.csproj src/Lalana.Application/
COPY src/Lalana.Infrastructure/Lalana.Infrastructure.csproj src/Lalana.Infrastructure/
COPY src/Lalana.Api/Lalana.Api.csproj src/Lalana.Api/
RUN dotnet restore src/Lalana.Api/Lalana.Api.csproj
COPY src/ src/
# The OpenAPI document is generated (and committed) by the normal build, not in the image.
RUN dotnet publish src/Lalana.Api/Lalana.Api.csproj -c Release -o /app --no-restore \
    -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lalana.Api.dll"]
