import axios, { type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';
import toast from 'react-hot-toast';
import keycloak from '../keycloak.ts';

/**
 * Request interceptor: добавляет токен авторизации из Keycloak.
 * Перед каждым запросом проверяем, не истек ли токен, и обновляем его при необходимости.
 */
export const requestInterceptor = async (
    config: InternalAxiosRequestConfig
): Promise<InternalAxiosRequestConfig> => {
    // Если токен скоро истечет (менее 5 секунд до конца), принудительно обновляем его
    if (keycloak.authenticated && keycloak.isTokenExpired(5)) {
        try {
            await keycloak.updateToken(5);
        } catch (error) {
            console.error('[Keycloak] Не удалось обновить токен, требуется повторный вход', error);
            keycloak.login(); // Принудительный логин, если refresh token тоже истек
        }
    }

    const token = keycloak.token;

    if (token) {
        config.headers = config.headers || {};
        config.headers.Authorization = `Bearer ${token}`;
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
 * Response error interceptor: обрабатывает ошибки (401, 403, 404, 500 и т.д.).
 * При 401 показывает тост и инициирует процесс повторного входа через Keycloak.
 */
export const responseErrorInterceptor = (error: any) => {
    if (error.response) {
        const { status, data } = error.response;

        if (status === 401) {
            toast.error('Сессия истекла. Перенаправление на страницу входа...');

            // Небольшая задержка, чтобы пользователь увидел тост, затем редирект в Keycloak
            setTimeout(() => {
                keycloak.login();
            }, 1000);

            return Promise.reject(error);
        }

        if (status === 403) {
            toast.error('У вас нет прав для выполнения этого действия.');
        } else if (status === 404) {
            console.warn('Ресурс не найден', data);
        } else {
            console.error(`Ошибка сервера (${status}):`, data);
        }
    } else if (error.request) {
        console.error('Сервер не отвечает:', error.request);
        toast.error('Сервер временно недоступен. Попробуйте позже.');
    } else {
        console.error('Ошибка при настройке запроса:', error.message);
    }

    return Promise.reject(error);
};