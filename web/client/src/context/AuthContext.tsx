/**
 * Реальный AuthContext, интегрированный с Keycloak.
 * Управляет состоянием аутентификации, извлекает данные пользователя из JWT-токена
 * и предоставляет методы для входа (login) и выхода (logout).
 */
import React, { createContext, useContext, useState, useEffect, useMemo, useCallback, type ReactNode } from 'react';
import keycloak from '../keycloak.ts';

/** Упрощённая модель пользователя (совпадает с UserDto из бэкенда) */
export interface UserDto {
    id: string;
    username: string;
    firstName: string;
    lastName: string;
    patronymic?: string;
    roles: string[];
}

interface AuthContextType {
    user: UserDto | null;
    isAuthenticated: boolean;
    isAdmin: boolean;
    isTcx: boolean;
    isAdminOrTcx: boolean;
    loading: boolean;
    login: () => void;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const ctx = useContext(AuthContext);
    if (!ctx) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return ctx;
};

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState<boolean>(keycloak.authenticated || false);
    const [loading, setLoading] = useState<boolean>(!keycloak.authenticated);

    useEffect(() => {
        // Обработчик успешного входа
        const onAuthSuccess = () => {
            setIsAuthenticated(true);
            setLoading(false);
        };

        // Обработчик выхода
        const onAuthLogout = () => {
            setIsAuthenticated(false);
            setLoading(false);
        };

        // Обработчик успешного обновления токена (refresh)
        const onAuthRefreshSuccess = () => {
            setIsAuthenticated(true);
        };

        // Обработчик ошибки обновления токена (сессия истекла)
        const onAuthRefreshError = () => {
            setIsAuthenticated(false);
        };

        // Подписываемся на события Keycloak
        keycloak.onAuthSuccess = onAuthSuccess;
        keycloak.onAuthLogout = onAuthLogout;
        keycloak.onAuthRefreshSuccess = onAuthRefreshSuccess;
        keycloak.onAuthRefreshError = onAuthRefreshError;

        // Если на момент монтирования компонента Keycloak уже инициализирован и авторизован
        if (keycloak.authenticated) {
            setIsAuthenticated(true);
            setLoading(false);
        } else {
            setLoading(false);
        }

        // Очистка обработчиков при размонтировании
        return () => {
            keycloak.onAuthSuccess = undefined;
            keycloak.onAuthLogout = undefined;
            keycloak.onAuthRefreshSuccess = undefined;
            keycloak.onAuthRefreshError = undefined;
        };
    }, []);

    // Функция принудительного входа (редирект на страницу логина Keycloak)
    const login = useCallback(() => {
        keycloak.login();
    }, []);

    // Функция выхода (очистка сессии в Keycloak и редирект)
    const logout = useCallback(() => {
        keycloak.logout();
    }, []);

    // Извлекаем данные пользователя из распарсенного JWT-токена Keycloak
    const user = useMemo<UserDto | null>(() => {
        if (!isAuthenticated || !keycloak.tokenParsed) return null;

        const token = keycloak.tokenParsed;

        // Keycloak хранит роли в token.realm_access.roles и token.resource_access.{client_id}.roles
        const realmRoles: string[] = token.realm_access?.roles || [];
        const clientRoles: string[] = token.resource_access?.[keycloak.clientId as string]?.roles || [];
        const allRoles = [...new Set([...realmRoles, ...clientRoles])];

        return {
            id: token.sub as string,
            username: token.preferred_username as string || 'unknown',
            firstName: token.given_name as string || '',
            lastName: token.family_name as string || '',
            patronymic: undefined, // Keycloak стандартно не хранит отчество
            roles: allRoles,
        };
    }, [isAuthenticated, keycloak.tokenParsed]);

    // Вычисляемые флаги ролей для удобной проверки прав в компонентах
    const isAdmin = useMemo(() => user?.roles.includes('Admin') || false, [user]);
    const isTcx = useMemo(() => user?.roles.includes('TCX') || false, [user]);
    const isAdminOrTcx = useMemo(() => isAdmin || isTcx, [isAdmin, isTcx]);

    const value: AuthContextType = {
        user,
        isAuthenticated,
        isAdmin,
        isTcx,
        isAdminOrTcx,
        loading,
        login,
        logout,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};