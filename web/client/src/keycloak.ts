import Keycloak from 'keycloak-js';

/**
 * Конфигурация подключения к Keycloak.
 * Значения берутся из переменных окружения Vite (если они заданы в .env файле) 
 * или используются дефолтные значения для локальной разработки.
 * 
 * Для локальной разработки:
 * - URL: http://localhost:8080
 * - Realm: humidity-realm
 * - Client ID: humidity-api-client
 */
const keycloakConfig = {
    url: import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8080',
    realm: import.meta.env.VITE_KEYCLOAK_REALM || 'humidity',
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'humidity-frontend',
};

/**
 * Инициализация экземпляра Keycloak.
 * Этот объект будет использоваться во всем приложении для получения токенов,
 * проверки статуса аутентификации и управления сессией пользователя.
 */
const keycloak = new Keycloak(keycloakConfig);

export default keycloak;