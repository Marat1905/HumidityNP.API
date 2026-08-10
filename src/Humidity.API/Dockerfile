# Этап 1: Базовый образ для запуска (ASP.NET Core Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8080

# Этап 2: Образ для сборки (SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файлы проектов для восстановления зависимостей (кэширование слоя)
COPY ["src/Humidity.API/Humidity.API.csproj", "src/Humidity.API/"]
COPY ["src/Humidity.Application/Humidity.Application.csproj", "src/Humidity.Application/"]
COPY ["src/Humidity.Domain/Humidity.Domain.csproj", "src/Humidity.Domain/"]
COPY ["src/Humidity.Infrastructure/Humidity.Infrastructure.csproj", "src/Humidity.Infrastructure/"]

# Восстанавливаем пакеты
RUN dotnet restore "src/Humidity.API/Humidity.API.csproj"

# Копируем весь исходный код
COPY . .

# Переходим в папку API проекта и собираем
WORKDIR "/src/src/Humidity.API"
RUN dotnet build "Humidity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап 3: Публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Humidity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этап 4: Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Humidity.API.dll"]