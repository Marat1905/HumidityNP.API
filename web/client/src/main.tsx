/**
 * Точка входа в приложение.
 * Инициализирует Keycloak до рендеринга React-приложения.
 * Это гарантирует, что к моменту первого рендера статус аутентификации уже известен.
 */
import React from 'react';
import { createRoot } from 'react-dom/client';
import keycloak from './keycloak.ts';
import App from './App.tsx';
import './index.css';

/**
 * Функция инициализации приложения.
 * Пытается выполнить аутентификацию через Keycloak. 
 * Используется режим 'check-sso', который проверяет наличие активной сессии 
 * через скрытый iframe (silent-check-sso.html).
 */
const initKeycloak = async () => {
    try {
        // Инициализация Keycloak
        await keycloak.init({
            onLoad: 'check-sso', // Проверять сессию, но не редиректить принудительно, если её нет
            silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
            pkceMethod: 'S256', // Использование PKCE для безопасности (защита от перехвата кода авторизации)
            checkLoginIframe: false, // Отключаем периодическую проверку iframe для экономии ресурсов
        });
    } catch (error) {
        console.error('[Keycloak] Ошибка инициализации:', error);
    }

    // Рендерим React-приложение независимо от результата инициализации.
    // Компоненты (через AuthContext) сами решат, показывать контент или требовать логин.
    createRoot(document.getElementById('root')!).render(
        <React.StrictMode>
            <App />
        </React.StrictMode>
    );
};

initKeycloak();