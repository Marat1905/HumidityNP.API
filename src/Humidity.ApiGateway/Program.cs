using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text.Json;

// ============================================================
// Точка входа API Gateway (YARP).
// Задачи:
//  1. Единая точка входа для фронтенда (порт 8090 в docker-compose).
//  2. Валидация JWT, выданного Keycloak, до попадания запроса в сервисы.
//  3. Проксирование REST-запросов на Humidity.API и Notification.Service.
//  4. Проброс user-контекста (sub, roles) во внутренние сервисы
//     через заголовки X-User-Id, X-User-Roles — упрощает жизнь внизу.
//  5. CORS для React-клиента.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. НАСТРОЙКА SERILOG
// ==============================================================================
// Конфигурируем Serilog для структурированного логирования с обогащением контекста, 
// имени машины и идентификатора потока. Это обеспечивает детальное отслеживание 
// всех запросов, проходящих через шлюз, что критически важно для диагностики 
// проблем в распределенной системе.
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day);
});

// ------------------------------------------------------------
// 2. Аутентификация через Keycloak (JWT Bearer)
// ------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority — адрес realm-а. MetadataAddress — явный путь к .well-known,
        // потому что внутри docker-сети authority может отличаться от внешнего.
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? $"{options.Authority}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = bool.Parse(
            builder.Configuration["Keycloak:RequireHttpsMetadata"] ?? "true");

        // Внутри docker-сети между сервисами используется HTTP, поэтому
        // audience не всегда приходит — валидируем только issuer и подпись.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"],
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

        // Не отдаём стандартный WWW-Authenticate challenge в HTML-формате,
        // чтобы фронтенд получал 401 JSON-ом.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        statusCode = 401,
                        message = "Unauthorized. Требуется действительный токен Keycloak."
                    }));
            }
        };
    });

builder.Services.AddAuthorization();

// ------------------------------------------------------------
// 3. CORS для React-клиента
// ------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:8090")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ==============================================================================
// 4. ДОБАВЛЕНИЕ СЕРВИСОВ YARP (REVERSE PROXY)
// ==============================================================================
// Загружаем конфигурацию маршрутов (Routes) и кластеров (Clusters) из секции 
// "ReverseProxy" в файле appsettings.json. YARP использует эти данные для 
// перенаправления входящих HTTP-запросов на соответствующие внутренние сервисы.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ==============================================================================
// 5. ДОБАВЛЕНИЕ HEALTH CHECKS
// ==============================================================================
// Добавляем базовую проверку состояния (health checks) для мониторинга 
// доступности самого шлюза. В дальнейшем сюда можно добавить проверки 
// доступности целевых кластеров (например, пинг основного API).
builder.Services.AddHealthChecks();

var app = builder.Build();

// ==============================================================================
// 6. НАСТРОЙКА КОНВЕЙЕРА ОБРАБОТКИ ЗАПРОСОВ (MIDDLEWARE PIPELINE)
// ==============================================================================
app.UseSerilogRequestLogging();

app.UseCors("FrontendPolicy");

// Валидируем токен, но не блокируем анонимные запросы:
// например, /health должен быть доступен без токена.
app.UseAuthentication();

// Пробрасываем идентификатор пользователя и его роли во внутренние
// сервисы через заголовки — упрощает построение audit-логов и
// позволяет микросервисам не парсить JWT повторно.
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.User.FindFirst("preferred_username")?.Value
                     ?? "unknown";
        var roles = string.Join(",", context.User.FindAll("roles").Select(c => c.Value));
        context.Request.Headers["X-User-Id"] = userId;
        context.Request.Headers["X-User-Roles"] = roles;
    }

    await next();
});

app.UseAuthorization();

// Добавляем конечную точку для проверки состояния (health checks) по пути /health.
// Это позволяет оркестраторам (например, Kubernetes или Docker Compose) 
// проверять, готов ли шлюз принимать трафик.
app.MapHealthChecks("/health");

// ==============================================================================
// 7. МАРШРУТИЗАЦИЯ ЗАПРОСОВ ЧЕРЕЗ YARP
// ==============================================================================
// Метод MapReverseProxy() добавляет конечные точки для всех маршрутов, 
// определенных в конфигурации. Это должно быть одним из последних вызовов 
// в конвейере, чтобы запросы могли пройти через другие middleware (например, 
// логирование или аутентификацию, если они будут добавлены в шлюз) перед проксированием.
app.MapReverseProxy();

// ==============================================================================
// 8. ИНИЦИАЛИЗАЦИЯ И ЗАПУСК ПРИЛОЖЕНИЯ
// ==============================================================================
try
{
    // Логируем успешный запуск шлюза. Этот лог гарантированно запишется, 
    // так как приложение успешно прошло все этапы инициализации и конфигурации.
    app.Logger.LogInformation("=== API GATEWAY УСПЕШНО ЗАПУЩЕН И ГОТОВ К ПРИЕМУ ЗАПРОСОВ ===");

    // Запускаем веб-сервер. Этот вызов блокирует выполнение до остановки приложения 
    // (например, при получении сигнала завершения от ОС или оркестратора).
    await app.RunAsync();
}
catch (Exception ex)
{
    // Логируем фатальную ошибку, если шлюз не смог запуститься из-за проблем 
    // с конфигурацией, сетью или других критических сбоев на старте.
    Log.Fatal(ex, "API Gateway startup failed");
}
finally
{
    // Закрываем и очищаем логгер при завершении работы приложения, 
    // гарантируя, что все буферизированные логи будут записаны в целевые хранилища.
    Log.CloseAndFlush();
}