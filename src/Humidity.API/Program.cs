using FluentValidation;
using FluentValidation.AspNetCore;
using Humidity.API.Middleware;
using Humidity.Application;
using Humidity.Application.Validators;
using Humidity.Infrastructure;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HealthChecks.NpgSql;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Serilog; // добавлен using для Serilog

var builder = WebApplication.CreateBuilder(args);

// 1. НАСТРОЙКА SERILOG (унифицированная)
// Конфигурация полностью читается из appsettings.json (секция "Serilog").
// Все WriteTo (Console, File) заданы там же, поэтому в коде их не дублируем.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog(); // заменяем стандартный логгер на Serilog

// 2. РЕГИСТРАЦИЯ FLUENT VALIDATION
// Автоматически находит все классы, наследующие AbstractValidator, в указанной сборке
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// Add services to the container.
builder.Services.AddControllers();

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

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Logging - уже настроен Serilog, дополнительная регистрация не требуется

// Health Checks with database connectivity check
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "PostgreSQL",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "postgresql" });

var app = builder.Build();

// 6. ПРИМЕНЕНИЕ CORS MIDDLEWARE
// Важно: размещаем после UseRouting, но до UseAuthorization и UseEndpoints
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

// Карта health checks с выводом подробной информации
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