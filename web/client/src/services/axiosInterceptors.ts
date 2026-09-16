import axios, { type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';
import toast from 'react-hot-toast';
import {
    getAccessToken,
    loginWithKeycloak,
    logoutFromKeycloak,
} from './keycloak';

/**
 * Request interceptor: добавляет access-токен Keycloak в заголовок Authorization.
 */
export const requestInterceptor = async (
    config: InternalAxiosRequestConfig
): Promise<InternalAxiosRequestConfig> => {
    try {
        // minValiditySeconds=30: если токен истекает раньше чем через 30 секунд,

        const token = await getAccessToken(30);

        if (token) {
            config.headers = config.headers || {};
            config.headers.Authorization = `Bearer ${token}`;
        }
    } catch (err) {
        // Не блокируем запрос: если токена нет, бэкенд вернёт 401,
        // и мы обработаем это в responseErrorInterceptor.
        console.warn('[API] Не удалось получить токен Keycloak', err);
    }

    // Для FormData не устанавливаем Content-Type вручную,
    // чтобы браузер сам добавил boundary.
    if (config.data instanceof FormData) {
        delete (config.headers as any)['Content-Type'];
    }

    return config;
};

export const requestErrorInterceptor = (error: any) => {
    console.error('[API Request Error]', error);
    return Promise.reject(error);
};

/**
 * Response interceptor: успешные ответы проходят без изменений.
 */
export const responseInterceptor = (response: AxiosResponse) => {
    return response;
};

/**
 * Response error interceptor: обрабатывает 401, 403, 404, 5xx и сетевые ошибки.
 */
export const responseErrorInterceptor = async (error: any) => {
    const originalRequest = error.config;

    if (error.response) {
        const { status, data } = error.response;

        // ============================================================
        // 401 Unauthorized — проблема с токеном
        // ============================================================
        if (status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                // Пытаемся обновить токен через Keycloak.
                const token = await getAccessToken(0);

                if (token) {
                    // Токен обновился — повторяем исходный запрос.
                    originalRequest.headers = originalRequest.headers || {};
                    originalRequest.headers.Authorization = `Bearer ${token}`;
                    return axios(originalRequest);
                }
            } catch {
                // Не удалось обновить — идём на logout.
            }

            // Обновление не удалось — refresh-токен истёк или пользователь
            // был разлогинен на стороне Keycloak.
            toast.error('Сессия истекла. Требуется повторный вход.');

            // Небольшая задержка, чтобы тост успел отрисоваться.
            setTimeout(() => {
                logoutFromKeycloak().catch(() => {
                    // Если и logout упал — принудительно редиректим на login.
                    loginWithKeycloak().catch(() => {
                        window.location.href = '/';
                    });
                });
            }, 1500);

            return Promise.reject(error);
        }

        // ============================================================
        // 403 Forbidden — прав нет
        // ============================================================
        if (status === 403) {
            toast.error('У вас нет прав для выполнения этого действия.');
        }
        // ============================================================
        // 404 Not Found — ресурс не найден
        // ============================================================
        else if (status === 404) {
            console.warn('[API] Ресурс не найден', data);
        }
        // ============================================================
        // 429 Too Many Requests — превышен rate-limit
        // ============================================================
        else if (status === 429) {
            toast.error('Слишком много запросов. Пожалуйста, подождите.');
        }
        // ============================================================
        // 5xx — ошибка сервера
        // ============================================================
        else if (status >= 500) {
            console.error(`[API] Ошибка сервера (${status}):`, data);
            toast.error('Ошибка сервера. Попробуйте позже.');
        }
        // ============================================================
        // Прочие 4xx
        // ============================================================
        else {
            console.error(`[API] Ошибка (${status}):`, data);
            const message = data?.message ?? 'Произошла ошибка при выполнении запроса.';
            toast.error(message);
        }
    }
    // ============================================================
    // Сетевая ошибка — сервер недоступен
    // ============================================================
    else if (error.request) {
        console.error('[API] Сервер не отвечает:', error.request);
        toast.error('Сервер временно недоступен. Попробуйте позже.');
    }
    // ============================================================
    // Ошибка конфигурации запроса
    // ============================================================
    else {
        console.error('[API] Ошибка настройки запроса:', error.message);
    }

    return Promise.reject(error);
};