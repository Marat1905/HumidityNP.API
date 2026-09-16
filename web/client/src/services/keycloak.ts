/**
 * Singleton-сервис для Keycloak.
 */
import Keycloak from 'keycloak-js';

/**
 * Единственный инстанс Keycloak на всё приложение.
 */
let instance: Keycloak | null = null;

/**
 * Промис инициализации. Хранится, чтобы повторные вызовы initKeycloak()
 * (в т.ч. из StrictMode) не запускали kc.init() заново.
 */
let initPromise: Promise<boolean> | null = null;

/**
 * Флаг: инициализация уже завершена.
 */
let initialized = false;

/**
 * Настройки Keycloak из .env.
 */
const KEYCLOAK_URL = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080';
const KEYCLOAK_REALM = import.meta.env.VITE_KEYCLOAK_REALM ?? 'humidity';
const KEYCLOAK_CLIENT_ID = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'humidity-frontend';

/**
 * Единая точка входа после успешного входа/выхода.
 */
const POST_LOGIN_REDIRECT = `${window.location.origin}/humidity`;

/**
 * Получить единственный инстанс Keycloak, создав его при необходимости.
 */
export const getOrCreateKeycloak = (): Keycloak => {
    if (!instance) {
        instance = new Keycloak({
            url: KEYCLOAK_URL,
            realm: KEYCLOAK_REALM,
            clientId: KEYCLOAK_CLIENT_ID,
        });
    }
    return instance;
};

/**
 * Инициализировать Keycloak ровно один раз.
 */
export const initKeycloak = (): Promise<boolean> => {
    if (initPromise) {
        return initPromise;
    }

    const kc = getOrCreateKeycloak();

    initPromise = kc.init({
        onLoad: 'login-required',
        checkLoginIframe: false,
        pkceMethod: 'S256',
        silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
        // После успешного входа Keycloak вернёт пользователя
        redirectUri: POST_LOGIN_REDIRECT,
    })
        .then((authenticated) => {
            initialized = true;
            return authenticated;
        })
        .catch((err) => {
            console.error('[Keycloak] init failed', err);
            // Сбрасываем промис, чтобы можно было повторить попытку.
            initPromise = null;
            throw err;
        });

    return initPromise;
};

/**
 * Флаг завершённой инициализации.
 */
export const isKeycloakInitialized = (): boolean => initialized;

/**
 * Установить инстанс извне (для тестов / SSR).
 */
export const setKeycloakInstance = (kc: Keycloak): void => {
    instance = kc;
};

/**
 * Получить инстанс Keycloak или null.
 */
export const getKeycloakInstance = (): Keycloak | null => instance;

/**
 * Получить актуальный access-токен, обновив его при необходимости.
 */
export const getAccessToken = async (minValiditySeconds = 30): Promise<string> => {
    if (!instance) return '';

    try {
        await instance.updateToken(minValiditySeconds);
        return instance.token ?? '';
    } catch {
        return '';
    }
};

/**
 * Принудительный редирект на форму входа Keycloak.
 */
export const loginWithKeycloak = async (): Promise<void> => {
    if (!instance) return;
    await instance.login({ redirectUri: POST_LOGIN_REDIRECT });
};

/**
 * Разлогинить пользователя.
 */
export const logoutFromKeycloak = async (
    redirectUri: string = POST_LOGIN_REDIRECT
): Promise<void> => {
    if (!instance) return;
    await instance.logout({ redirectUri });
};