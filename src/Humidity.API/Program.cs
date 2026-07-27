using Asp.Versioning;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.NpgSql;
using Humidity.API.BackgroundServices;
using Humidity.API.Middleware;
using Humidity.Application;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;
using Humidity.Application.Validators;
using Humidity.Infrastructure;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. НАСТРОЙКА SERILOG
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. РЕГИСТРАЦИЯ FLUENT VALIDATION
// Автоматически находит все классы, наследующие AbstractValidator, в указанной сборке
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Сериализуем все enum как строки ("Auto", "Manual", "Less", "Greater", "None")
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Опционально: формат camelCase для имен свойств (опционально, зависит от фронтенда)
        // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 3. НАСТРОЙКА ВЕРСИОНИРОВАНИЯ API
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

// 4. НАСТРОЙКА SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Humidity API", Version = "v1" });
    // Добавляем фильтр для замены {version} в путях
    c.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
});

// 5. НАСТРОЙКА CORS
// Читаем настройки CORS из конфигурации

var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>()
                   ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        // Если в режиме разработки и список разрешённых источников не задан — разрешаем любые
        if (builder.Environment.IsDevelopment() && corsSettings.AllowedOrigins == null)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // В продакшене используем строго определённые источники, методы и заголовки
            policy.WithOrigins(corsSettings.AllowedOrigins ?? Array.Empty<string>())
                  .WithMethods(corsSettings.AllowedMethods ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" })
                  .WithHeaders(corsSettings.AllowedHeaders ?? new[] { "Content-Type", "Authorization", "X-Requested-With" })
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(corsSettings.PreflightMaxAgeMinutes ?? 10));
        }
    });
});

// ========== НАСТРОЙКА RATE LIMITING ==========
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// ========== РЕГИСТРАЦИЯ СЛОЁВ ==========
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ========== НАСТРОЙКА ИНТЕГРАЦИИ С 1С ==========
// Регистрация настроек
builder.Services.Configure<OneCIntegrationSettings>(
    builder.Configuration.GetSection("OneCIntegration"));

// Регистрация HTTP-клиента для 1С с политикой повторных попыток
builder.Services.AddHttpClient<IOneCClient, OneCClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<OneCIntegrationSettings>>().Value;
    client.BaseAddress = new Uri(settings.ServiceUrl);

    // Базовая аутентификация
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
        .HandleTransientHttpError() // обрабатывает HTTP 5xx, 408, HttpRequestException
        .OrResult(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500) // явно серверные ошибки
        .WaitAndRetryAsync(
            settings.RetryCount,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * settings.RetryBaseDelaySeconds),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                logger.LogWarning("Попытка {RetryCount} вызова 1С не удалась, повтор через {Delay:F0} мс. Ошибка: {Error}",
                    retryCount,
                    timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });
});

// Регистрация фонового сервиса синхронизации
builder.Services.AddHostedService<OneCSyncBackgroundService>();

// Logging - уже настроен Serilog, дополнительная регистрация не требуется

// Health Checks with database connectivity check
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "PostgreSQL",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "postgresql" });

var app = builder.Build();

// ========== ДОБАВЛЯЕМ ПРОМЕЖУТОЧНОЕ ПО RATE LIMITING ==========
// Должно быть добавлено до других middleware, но после использования CORS.
app.UseIpRateLimiting();

app.UseCors("AllowSpecificOrigins");

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Humidity API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseAuthorization();
app.MapControllers();

// Карта health checks
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

try
{
    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HumidityDbContext>();

        if (app.Environment.IsDevelopment())
        {
            // Только для разработки - пересоздание БД
            context.Database.EnsureCreated();
            await DataSeeder.SeedAsync(context);
        }
        else
        {
            // Для production - применяем миграции
            context.Database.Migrate();
        }
    }
}
catch (Exception ex)
{
    // Используем Serilog для логирования фатальной ошибки
    Log.Fatal(ex, "Database initialization failed");
}
finally
{
    // Обеспечиваем корректное завершение работы логгера
    Log.CloseAndFlush();
}

app.Run();

/// <summary>
/// Фильтр для Swagger, который заменяет {version} в пути на актуальное значение версии.
/// </summary>
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