# Этап 1: базовый образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Этап 2: сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Humidity.ApiGateway/Humidity.ApiGateway.csproj", "src/Humidity.ApiGateway/"]
RUN dotnet restore "src/Humidity.ApiGateway/Humidity.ApiGateway.csproj"

COPY . .
WORKDIR "/src/src/Humidity.ApiGateway"
RUN dotnet build "Humidity.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап 3: публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Humidity.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этап 4: финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Humidity.ApiGateway.dll"]