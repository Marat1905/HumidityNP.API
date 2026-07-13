using FluentValidation;
using FluentValidation.AspNetCore;
using Humidity.Application;
using Humidity.Application.Validators;
using Humidity.Infrastructure;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. РЕГИСТРАЦИЯ FLUENT VALIDATION
// Автоматически находит все классы, наследующие AbstractValidator, в указанной сборке
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Logging
builder.Services.AddLogging();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Humidity API v1");
        c.RoutePrefix = string.Empty;
    });
    app.UseSwagger();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

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