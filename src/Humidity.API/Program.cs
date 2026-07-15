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

var builder = WebApplication.CreateBuilder(args);

// 1. РЕГИСТРАЦИЯ FLUENT VALIDATION
// Автоматически находит все классы, наследующие AbstractValidator, в указанной сборке
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// Add services to the container.
builder.Services.AddControllers();

// 2. НАСТРОЙКА ВЕРСИОНИРОВАНИЯ API
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

// 3. НАСТРОЙКА SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Humidity API", Version = "v1" });
    // Добавляем фильтр для замены {version} в путях
    c.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
});

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Logging
builder.Services.AddLogging();

// Health Checks with database connectivity check
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "PostgreSQL",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "postgresql" });

var app = builder.Build();

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
    Console.WriteLine($"Database initialization failed: {ex.Message}");
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