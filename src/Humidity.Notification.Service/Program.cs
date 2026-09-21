using Humidity.Contracts.Protos;
using Humidity.Notification.Service.Data;
using Humidity.Notification.Service.gRPC;
using Humidity.Notification.Service.Options;
using Humidity.Notification.Service.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// 1. Serilog — структурное логирование
// ------------------------------------------------------------
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/notification-.txt", rollingInterval: RollingInterval.Day);
});

// ------------------------------------------------------------
// 2. Options
// ------------------------------------------------------------
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<RecipientsOptions>(builder.Configuration.GetSection(RecipientsOptions.SectionName));
builder.Services.Configure<NotificationServiceOptions>(builder.Configuration.GetSection(NotificationServiceOptions.SectionName));

// ------------------------------------------------------------
// 3. MongoDbContext + сервисы
// ------------------------------------------------------------
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<ShiftReportHtmlBuilder>();

// ------------------------------------------------------------
// 4. gRPC-сервер (для внешних потребителей журнала)
// ------------------------------------------------------------
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;
});

// ------------------------------------------------------------
// 5. gRPC-клиент для обращения к Humidity.API
// ------------------------------------------------------------
var apiGrpcAddress = builder.Configuration["HumidityApi:GrpcAddress"] ?? "http://localhost:5001";
builder.Services
    .AddGrpcClient<MeasurementGrpc.MeasurementGrpcClient>(options =>
    {
        options.Address = new Uri(apiGrpcAddress);
    });

// ------------------------------------------------------------
// 6. Фоновый сервис-потребитель RabbitMQ
// ------------------------------------------------------------
builder.Services.AddHostedService<ShiftEndedConsumer>();

// ------------------------------------------------------------
// 7. Health Checks + Swagger (для отладки)
// ------------------------------------------------------------
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

// gRPC-маппинг
app.MapGrpcService<NotificationGrpcService>();

// Health
app.MapHealthChecks("/health");

// Swagger (только для разработки)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Информационный корень
app.MapGet("/", () => new
{
    service = "Humidity.Notification.Service",
    status = "running",
    grpc = new[]
    {
        "NotificationService.GetNotificationLogs",
        "NotificationService.SendTestNotification"
    }
});

app.Logger.LogInformation("=== Humidity.Notification.Service запущен ===");

await app.RunAsync();