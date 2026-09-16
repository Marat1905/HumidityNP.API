using Asp.Versioning;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Humidity.API.Auth;
using Humidity.API.BackgroundServices;
using Humidity.API.Hubs;
using Humidity.API.Middleware;
using Humidity.API.Services;
using Humidity.Application;
using Humidity.Application.Common.Models;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;
using Humidity.Application.Validators;
using Humidity.Contracts.Protos;
using Humidity.Infrastructure;
using Humidity.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. НАСТРОЙКА SERILOG
// ==============================================================================
// Конфигурируем Serilog для структурированного логирования с обогащением контекста, 
// имени машины и идентификатора потока. Это обеспечивает детальное отслеживание 
// всех запросов и событий аутентификации.

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId();
});

// ==============================================================================
// 2. РЕГИСТРАЦИЯ FLUENT VALIDATION
// ==============================================================================
// Автоматически находит все классы, наследующие AbstractValidator, в указанной сборке
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// ==============================================================================
// 3. НАСТРОЙКА КОНТРОЛЛЕРОВ И СЕРИАЛИЗАЦИИ
// ==============================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Сериализуем все enum как строки ("Auto", "Manual", "Less", "Greater", "None")
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ==============================================================================
// 4. НАСТРОЙКА ВЕРСИОНИРОВАНИЯ API
// ==============================================================================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ==============================================================================
// 5. НАСТРОЙКА SWAGGER
// ==============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Humidity API",
        Version = "v1",
        Description = "API контроля влажности макулатуры. " +
                      "Аутентификация — Keycloak (OIDC). Real-time — SignalR."
    });

    // Схема безопасности Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Введите JWT-токен, полученный от Keycloak."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
});

// ==============================================================================
// 6. НАСТРОЙКА CORS
// ==============================================================================
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>()
    ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment() && corsSettings.AllowedOrigins == null)
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsSettings.AllowedOrigins ?? Array.Empty<string>())
                  .WithMethods(corsSettings.AllowedMethods ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" })
                  .WithHeaders(corsSettings.AllowedHeaders ?? new[] { "Content-Type", "Authorization", "X-Requested-With" })
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(corsSettings.PreflightMaxAgeMinutes ?? 10));
        }
    });
});

// ==============================================================================
// 7. НАСТРОЙКА RATE LIMITING
// ==============================================================================
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// ==============================================================================
// 8. РЕГИСТРАЦИЯ СЛОЁВ И ЗАВИСИМОСТЕЙ
// ==============================================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? $"{options.Authority}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = bool.Parse(
            builder.Configuration["Keycloak:RequireHttpsMetadata"] ?? "true");

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

        // Для SignalR-клиентов токен может приходить в query string (?access_token=)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/humidity"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicAuthorizationPolicyProvider>();

// ============================================================
// 9. SignalR
// ============================================================

var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IHumidityRealtimeNotifier, HumidityRealtimeNotifier>();

// ============================================================
// 10. RabbitMQ
// ============================================================
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// ============================================================
// 11. Слои приложения и инфраструктуры
// ============================================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// 12. gRPC-сервер (MeasurementGrpc для Notification.Service)
// ============================================================
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;
});

// ==============================================================================
// 13. НАСТРОЙКА ИНТЕГРАЦИИ С 1С
// ==============================================================================
builder.Services.Configure<OneCIntegrationSettings>(
    builder.Configuration.GetSection("OneCIntegration"));

builder.Services.AddHttpClient<IOneCClient, OneCClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<OneCIntegrationSettings>>().Value;
    client.BaseAddress = new Uri(settings.ServiceUrl);

    var byteArray = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
})
.AddPolicyHandler((serviceProvider, request) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<OneCIntegrationSettings>>().Value;
    var logger = serviceProvider.GetRequiredService<ILogger<OneCClient>>();

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TaskCanceledException>()
        .OrResult(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500)
        .WaitAndRetryAsync(
            settings.RetryCount,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * settings.RetryBaseDelaySeconds),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                logger.LogWarning("Попытка {RetryCount} вызова 1С не удалась, повтор через {Delay:F0} мс. Ошибка: {Error}",
                    retryCount, timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });
});

// ============================================================
// 14. Фоновые сервисы
// ============================================================
builder.Services.AddHostedService<OneCSyncBackgroundService>();

// ==============================================================================
// 15. HEALTH CHECKS
// ==============================================================================
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "PostgreSQL",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "postgresql" });

//builder.Services.AddCustomJWTAuthentification();

// ============================================================
// 16. Сборка приложения
// ============================================================
var app = builder.Build();

// ==============================================================================
// 17. MIDDLEWARE PIPELINE
// ==============================================================================
app.UseIpRateLimiting();
app.UseCors("AllowSpecificOrigins");
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Humidity API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Порядок важен: аутентификация должна идти перед авторизацией
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR
app.MapHub<HumidityHub>("/hubs/humidity");

// Health
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// ==============================================================================
// 18. ИНИЦИАЛИЗАЦИЯ И ЗАПУСК
// ==============================================================================
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HumidityDbContext>();
        context.Database.Migrate();
    }

    // 2. Логируем успешный запуск (теперь этот лог ГАРАНТИРОВАННО запишется)
    app.Logger.LogInformation("=== ПРИЛОЖЕНИЕ УСПЕШНО ЗАПУЩЕНО И ГОТОВО К РАБОТЕ ===");

    // 3. Запускаем веб-сервер (блокирует выполнение до остановки приложения)
    await app.RunAsync();
}
catch (Exception ex)
{
    // Логируем фатальную ошибку, если приложение не смогло запуститься
    Log.Fatal(ex, "Application startup or database initialization failed");
}
finally
{
    // 4. Закрываем логгер ТОЛЬКО при реальном завершении работы приложения (например, по Ctrl+C)
    Log.CloseAndFlush();
}

// ==============================================================================
// ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ
// ==============================================================================
public class ReplaceVersionWithExactValueInPathFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();
        foreach (var path in swaggerDoc.Paths)
        {
            // Заменяем {version} в ключе пути на фактическую версию из документации
            var newKey = path.Key.Replace("{version}", swaggerDoc.Info.Version);
            paths.Add(newKey, path.Value);
        }
        swaggerDoc.Paths = paths;
    }
}

/// <summary>
/// Настройки CORS, читаемые из appsettings.json.
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Массив разрешённых источников (например, https://myfrontend.com).
    /// </summary>
    public string[]? AllowedOrigins { get; set; }

    /// <summary>
    /// Массив разрешённых HTTP-методов (если не указан, используются GET, POST, PUT, DELETE, OPTIONS).
    /// </summary>
    public string[]? AllowedMethods { get; set; }

    /// <summary>
    /// Массив разрешённых заголовков (если не указан, используются Content-Type, Authorization, X-Requested-With).
    /// </summary>
    public string[]? AllowedHeaders { get; set; }

    /// <summary>
    /// Время кеширования предварительного запроса (preflight) в минутах.
    /// </summary>
    public int? PreflightMaxAgeMinutes { get; set; }
}