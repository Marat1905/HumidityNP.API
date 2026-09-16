/**
 * AuthContext на базе Keycloak (keycloak-js).
 */
import React, {
    createContext,
    useContext,
    useState,
    useEffect,
    useMemo,
    useCallback,
    useRef,
    type ReactNode,
} from 'react';
import type { KeycloakTokenParsed } from 'keycloak-js';
import { createSignalRClient, type HumiditySignalRClient } from '../services/signalr';
import {
    getOrCreateKeycloak,
    initKeycloak,
    getAccessToken,
    logoutFromKeycloak,
} from '../services/keycloak';

/**
 * Расширенный набор полей JWT, которые отдаёт Keycloak.
 */
export interface KeycloakRealmAccess {
    roles: string[];
}

export interface ParsedToken extends KeycloakTokenParsed {
    sub: string;
    preferred_username: string;
    given_name?: string;
    family_name?: string;
    email?: string;
    realm_access?: KeycloakRealmAccess;
}

export interface UserDto {
    id: string;
    username: string;
    firstName: string;
    lastName: string;
    email?: string;
    roles: string[];
}

interface AuthContextType {
    user: UserDto | null;
    isAuthenticated: boolean;
    isAdmin: boolean;
    isTcx: boolean;
    isAdminOrTcx: boolean;
    getToken: () => Promise<string>;
    logout: () => Promise<void>;
    hasRole: (role: string) => boolean;
    loading: boolean;
    signalR: HumiditySignalRClient | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const ctx = useContext(AuthContext);
    if (!ctx) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return ctx;
};

const TOKEN_REFRESH_INTERVAL_MS = 30_000;
const TOKEN_MIN_VALIDITY_SECONDS = 60;

/**
 * Сравнение двух пользователей по значимым полям.
 * Нужно, чтобы не создавать новый объект user, если данные не изменились.
 */
const isSameUser = (a: UserDto | null, b: UserDto | null): boolean => {
    if (a === b) return true;
    if (!a || !b) return false;

    return (
        a.id === b.id &&
        a.username === b.username &&
        a.firstName === b.firstName &&
        a.lastName === b.lastName &&
        a.email === b.email &&
        a.roles.length === b.roles.length &&
        a.roles.every((r, i) => r === b.roles[i])
    );
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [user, setUserState] = useState<UserDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [signalR, setSignalR] = useState<HumiditySignalRClient | null>(null);

    const refreshIntervalRef = useRef<number | null>(null);

    /**
     * Безопасный сеттер: не создаёт новый объект, если пользователь
     * не изменился. Предотвращает лишние ре-рендеры и перезапуск
     * SignalR-эффекта при обновлении токена.
     */
    const setUser = useCallback((next: UserDto | null) => {
        setUserState(prev => (isSameUser(prev, next) ? prev : next));
    }, []);

    /**
     * Извлечение информации о пользователе из распарсенного JWT.
     */
    const extractUser = useCallback((tokenParsed: ParsedToken): UserDto => {
        const roles = tokenParsed.realm_access?.roles ?? [];
        return {
            id: tokenParsed.sub,
            username: tokenParsed.preferred_username,
            firstName: tokenParsed.given_name ?? '',
            lastName: tokenParsed.family_name ?? '',
            email: tokenParsed.email,
            roles: [...roles].sort(), // стабильный порядок для сравнения
        };
    }, []);

    // ============================================================
    // 1. Инициализация Keycloak — через singleton, идемпотентно.
    // ============================================================
    useEffect(() => {
        let cancelled = false;

        initKeycloak()
            .then((authenticated) => {
                if (cancelled) return;

                const kc = getOrCreateKeycloak();
                if (authenticated && kc.tokenParsed) {
                    setUser(extractUser(kc.tokenParsed as ParsedToken));
                }
            })
            .catch((err) => {
                if (cancelled) return;
                console.error('[Auth] Keycloak init error', err);
            })
            .finally(() => {
                if (cancelled) return;
                setLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [extractUser, setUser]);

    // ============================================================
    // 2. Фоновое обновление токена.
    // Запускается только после того, как Keycloak инициализирован.
    // ============================================================
    useEffect(() => {
        if (loading) return;

        const kc = getOrCreateKeycloak();
        if (!kc.authenticated) return;

        refreshIntervalRef.current = window.setInterval(async () => {
            try {
                await kc.updateToken(TOKEN_MIN_VALIDITY_SECONDS);
                if (kc.tokenParsed) {
                    // setUser пропустит ре-рендер, если данные не изменились.
                    setUser(extractUser(kc.tokenParsed as ParsedToken));
                }
            } catch (err) {
                console.warn('[Auth] Не удалось обновить токен', err);
                await kc.logout();
            }
        }, TOKEN_REFRESH_INTERVAL_MS);

        // Обработчик истечения токена — срабатывает, если между тиками
        // токен всё-таки протух.
        kc.onTokenExpired = async () => {
            try {
                await kc.updateToken(30);
            } catch {
                await kc.logout();
            }
        };

        return () => {
            if (refreshIntervalRef.current !== null) {
                clearInterval(refreshIntervalRef.current);
                refreshIntervalRef.current = null;
            }
        };
    }, [loading, extractUser, setUser]);

    // ============================================================
    // 3. SignalR-клиент.
    //
    // ВАЖНО: зависимость от user?.id, а не от user целиком.
    // Иначе каждое обновление токена (каждые 30 сек) будет
    // останавливать и пересоздавать SignalR — экран мерцает.
    // ============================================================
    const userId = user?.id ?? null;

    useEffect(() => {
        if (!userId) return;

        const kc = getOrCreateKeycloak();
        if (!kc.authenticated) return;

        let cancelled = false;
        const client = createSignalRClient(async () => {
            await kc.updateToken(30);
            return kc.token ?? '';
        });

        client.start()
            .then(() => {
                if (!cancelled) setSignalR(client);
            })
            .catch(err => console.error('[Auth] SignalR start error', err));

        return () => {
            cancelled = true;
            client.stop().catch(() => { /* игнорируем */ });
            setSignalR(null);
        };
    }, [userId]);

    // ============================================================
    // 4. Публичные методы.
    // ============================================================
    const getToken = useCallback(async (): Promise<string> => {
        return getAccessToken(30);
    }, []);

    const logout = useCallback(async (): Promise<void> => {
        await logoutFromKeycloak(window.location.origin);
    }, []);

    const hasRole = useCallback((role: string): boolean => {
        return user?.roles.includes(role) ?? false;
    }, [user]);

    // ============================================================
    // 5. Вычисляемые флаги ролей.
    // ============================================================
    const isAdmin = useMemo(() => hasRole('Admin'), [hasRole]);
    const isTcx = useMemo(() => hasRole('TCX'), [hasRole]);
    const isAdminOrTcx = useMemo(() => isAdmin || isTcx, [isAdmin, isTcx]);

    // ============================================================
    // 6. Значение контекста.
    // ============================================================
    const value: AuthContextType = useMemo(() => ({
        user,
        isAuthenticated: !!user,
        isAdmin,
        isTcx,
        isAdminOrTcx,
        getToken,
        logout,
        hasRole,
        loading,
        signalR,
    }), [user, isAdmin, isTcx, isAdminOrTcx, getToken, logout, hasRole, loading, signalR]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};