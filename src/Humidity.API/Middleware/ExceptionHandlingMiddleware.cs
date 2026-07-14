using Humidity.Application.Common.Models;
using System.Diagnostics;
using System.Net;

namespace Humidity.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        // Обработка специфичных типов исключений
        switch (exception)
        {
            case BadHttpRequestException badRequestEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = context.Response.StatusCode;
                response.Message = badRequestEx.Message;
                break;

            case KeyNotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusCode = context.Response.StatusCode;
                response.Message = notFoundEx.Message;
                break;

            case FluentValidation.ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = context.Response.StatusCode;
                response.Message = "Ошибка валидации данных";
                response.Errors = validationEx.Errors.Select(e => e.ErrorMessage);
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.StatusCode = context.Response.StatusCode;
                response.Message = "Ошибка при сохранении данных в базу (возможно, нарушение уникальности).";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = context.Response.StatusCode;
                response.Message = "Произошла внутренняя ошибка сервера";

                // ВАЖНО: Возвращаем детали и StackTrace ТОЛЬКО в режиме разработки
                if (_env.IsDevelopment())
                {
                    response.Details = exception.Message;
                    // response.Details += $"\n{exception.StackTrace}"; // Раскомментируйте при острой необходимости
                }
                break;
        }

        await context.Response.WriteAsJsonAsync(response);
    }
}