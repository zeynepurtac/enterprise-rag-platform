# ---- Build stage --------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching, then restore.
COPY RagPlatform.sln ./
COPY src/RagPlatform.Domain/RagPlatform.Domain.csproj src/RagPlatform.Domain/
COPY src/RagPlatform.Application/RagPlatform.Application.csproj src/RagPlatform.Application/
COPY src/RagPlatform.Infrastructure/RagPlatform.Infrastructure.csproj src/RagPlatform.Infrastructure/
COPY src/RagPlatform.Api/RagPlatform.Api.csproj src/RagPlatform.Api/
RUN dotnet restore RagPlatform.sln

# Copy the rest of the source and publish a self-contained framework build.
COPY . .
RUN dotnet publish src/RagPlatform.Api/RagPlatform.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create the uploads directory owned by the non-root runtime user.
RUN mkdir -p /app/data/uploads && chown -R app:app /app/data
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "RagPlatform.Api.dll"]
