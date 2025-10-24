FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["ProfanityService/ProfanityService.csproj", "ProfanityService/"]

# Restore dependencies
RUN dotnet restore "ProfanityService/ProfanityService.csproj"

# Copy everything
COPY . .

# Build
WORKDIR /src/ProfanityService
RUN dotnet build "ProfanityService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
WORKDIR /src/ProfanityService
RUN dotnet publish "ProfanityService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]
