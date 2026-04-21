# Stage 1 – build & publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY Keyless.sln .
COPY Keyless.API/Keyless.API.csproj                         Keyless.API/
COPY Keyless.Application/Keyless.Application.csproj         Keyless.Application/
COPY Keyless.Domain/Keyless.Domain.csproj                   Keyless.Domain/
COPY Keyless.Infrastructure/Keyless.Infrastructure.csproj   Keyless.Infrastructure/
COPY Keyless.Shared/Keyless.Shared.csproj                   Keyless.Shared/
COPY Keyless.Tests/Keyless.Tests.csproj                     Keyless.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish Keyless.API/Keyless.API.csproj \
        -c Release \
        -o /app/publish \
        --no-restore

# Stage 2 – runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# Bind to port 8080 (non-root-friendly default for containers)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Keyless.API.dll"]
