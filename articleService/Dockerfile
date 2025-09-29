FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files
COPY ["ArticleService/ArticleService.csproj", "ArticleService/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]

# Restore dependencies
RUN dotnet restore "ArticleService/ArticleService.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR /src/ArticleService
RUN dotnet build "ArticleService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
WORKDIR /src/ArticleService
RUN dotnet publish "ArticleService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArticleService.dll"]
