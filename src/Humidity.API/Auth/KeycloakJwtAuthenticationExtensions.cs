using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Humidity.API.Auth;

/// <summary>
/// Класс расширений для настройки аутентификации JWT с использованием сервера Keycloak.
/// Этот подход использует метаданные OpenID Connect (.well-known/openid-configuration) 
/// для автоматического получения актуальных ключей подписи (JWKS), что обеспечивает 
/// высокую безопасность и корректную ротацию ключей без необходимости перезапуска приложения.
/// </summary>
public static class KeycloakJwtAuthenticationExtensions
{
    /// <summary>
    /// Добавляет и настраивает аутентификацию JWT Bearer на основе настроек Keycloak из конфигурации.
    /// </summary>
    /// <param name="services">Коллекция сервисов для внедрения зависимостей.</param>
    /// <param name="configuration">Конфигурация приложения для чтения параметров Keycloak.</param>
    /// <returns>Коллекция сервисов для дальнейшей цепочки вызовов.</returns>
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Чтение секции конфигурации, специфичной для Keycloak
        var keycloakSection = configuration.GetSection("Authorization:Keycloak");

        var realm = keycloakSection.GetValue<string>("Realm") ?? "humidity-realm";
        var authServerUrl = keycloakSection.GetValue<string>("AuthServerUrl") ?? "http://keycloak:8080";
        var metadataAddress = keycloakSection.GetValue<string>("MetadataAddress") ?? $"{authServerUrl}/realms/{realm}/.well-known/openid-configuration";
        var requireHttpsMetadata = keycloakSection.GetValue<bool>("RequireHttpsMetadata");
        var validAudiences = keycloakSection.GetSection("ValidAudiences").Get<List<string>>() ?? new List<string> { "account" };

        // Настройка схемы аутентификации JwtBearer
        services.AddAuthentication(options =>
        {
            // Устанавливаем JWT Bearer как схему аутентификации по умолчанию
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // URL для получения метаданных OpenID Connect (содержит issuer, JWKS URI и т.д.)
            options.MetadataAddress = metadataAddress;

            // Отключаем требование HTTPS только для локальной разработки. 
            // В продакшн-среде это должно быть строго true.
            options.RequireHttpsMetadata = requireHttpsMetadata;

            // Дополнительная тонкая настройка параметров валидации токена
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Проверка издателя токена (должен совпадать с Realm URL Keycloak)
                ValidateIssuer = keycloakSection.GetValue<bool>("ValidateIssuer"),

                // Проверка аудитории токена (client_id, для которого выпущен токен)
                ValidateAudience = keycloakSection.GetValue<bool>("ValidateAudience"),
                ValidAudiences = validAudiences,

                // Проверка срока действия токена (exp и nbf claims)
                ValidateLifetime = keycloakSection.GetValue<bool>("ValidateLifetime"),

                // Проверка подписи токена с использованием ключей из JWKS
                ValidateIssuerSigningKey = keycloakSection.GetValue<bool>("ValidateIssuerSigningKey"),

                // Игнорирование ошибок времени при проверке срока действия (допуск 5 минут)
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Обработчик событий аутентификации для логирования и отладки
            options.Events = new JwtBearerEvents
            {
                // Вызывается при возникновении ошибки аутентификации
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        // Добавляем заголовок в ответ, чтобы клиент знал, что токен истек
                        context.Response.Headers.Append("Token-Expired", "true");
                    }

                    // Логирование ошибки (используем стандартный логгер, который будет перехвачен Serilog)
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning(context.Exception, "Ошибка аутентификации JWT: {Message}", context.Exception.Message);

                    return Task.CompletedTask;
                },

                // Вызывается при отсутствии заголовка Authorization или неверной схеме
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("Вызов аутентификации не удался. Ошибка: {Error}, Описание: {Description}",
                        context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}