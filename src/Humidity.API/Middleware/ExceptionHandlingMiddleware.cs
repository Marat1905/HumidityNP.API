using Humidity.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

            case DbUpdateException dbEx:
                HandleDbUpdateException(context, response, dbEx);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = context.Response.StatusCode;
                response.Message = "Произошла внутренняя ошибка сервера";

                if (_env.IsDevelopment())
                {
                    response.Details = exception.Message;
                }
                break;
        }

        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Обрабатывает исключения, связанные с обновлением базы данных, извлекая код ошибки PostgreSQL
    /// и формируя понятное сообщение для клиента.
    /// </summary>
    private void HandleDbUpdateException(HttpContext context, ErrorResponse response, DbUpdateException dbEx)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
        response.StatusCode = context.Response.StatusCode;

        // Получаем внутреннее исключение PostgreSQL
        var postgresEx = dbEx.InnerException as PostgresException;
        if (postgresEx != null)
        {
            // Сопоставляем код ошибки PostgreSQL с сообщением
            response.Message = postgresEx.SqlState switch
            {
                "23505" => "Нарушение уникальности: запись с такими данными уже существует.",
                "23503" => "Нарушение внешнего ключа: невозможно удалить или изменить связанные данные.",
                "23514" => "Нарушение ограничения CHECK: значение не удовлетворяет условию.",
                "23502" => "Нарушение NOT NULL: обязательное поле не заполнено.",
                "23000" => "Общая ошибка целостности данных.",
                _ => "Ошибка при сохранении данных в базу."
            };

            // В режиме разработки добавляем детали ошибки для отладки
            if (_env.IsDevelopment())
            {
                response.Details = $"PostgreSQL Error Code: {postgresEx.SqlState}, Message: {postgresEx.MessageText}";
                if (!string.IsNullOrEmpty(postgresEx.Detail))
                    response.Details += $", Detail: {postgresEx.Detail}";
            }
        }
        else
        {
            // Если не удалось распознать специфичную ошибку PostgreSQL
            response.Message = "Ошибка при сохранении данных в базу.";
            if (_env.IsDevelopment())
            {
                response.Details = dbEx.InnerException?.Message ?? dbEx.Message;
            }
        }
    }
}