# syntax=docker/dockerfile:1

# --- Build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy just the project files first so `dotnet restore` is cached across builds until a
# .csproj actually changes, instead of being invalidated by every source edit.
COPY ["src/PixDynamicGallery.Domain/PixDynamicGallery.Domain.csproj", "src/PixDynamicGallery.Domain/"]
COPY ["src/PixDynamicGallery.Application/PixDynamicGallery.Application.csproj", "src/PixDynamicGallery.Application/"]
COPY ["src/PixDynamicGallery.Infrastructure/PixDynamicGallery.Infrastructure.csproj", "src/PixDynamicGallery.Infrastructure/"]
COPY ["src/PixDynamicGallery.Api/PixDynamicGallery.Api.csproj", "src/PixDynamicGallery.Api/"]
RUN dotnet restore "src/PixDynamicGallery.Api/PixDynamicGallery.Api.csproj"

COPY . .
WORKDIR /src/src/PixDynamicGallery.Api
RUN dotnet build "PixDynamicGallery.Api.csproj" -c Release -o /app/build --no-restore

# --- Publish ---------------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "PixDynamicGallery.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- Runtime ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# The mcr.microsoft.com/dotnet/aspnet base image already ships a non-root "app" user/group —
# just switch to it instead of creating a duplicate.
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PixDynamicGallery.Api.dll"]
