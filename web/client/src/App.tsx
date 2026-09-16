/**
 * Главный компонент приложения (humidity).
 * Управляет темой оформления (светлая/тёмная) и отображает глобальные уведомления.
 * Содержит кнопки в правом верхнем углу: профиль пользователя, выход и переключение темы.
 */
import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { Toaster } from 'react-hot-toast';
import { FiSun, FiMoon, FiLogOut, FiUser } from 'react-icons/fi';
import Layout from './components/humidity/Layout';
import { HumidityPage, VehicleDetailsPage } from './pages/Humidity';
import { AuthProvider, useAuth } from './context/AuthContext';
import './index.css';

/**
 * Внутренний компонент — внутри AuthProvider,
 * поэтому может использовать useAuth().
 */
const AppInner: React.FC = () => {
    // Состояние темы: 'light' или 'dark'.
    // Приоритет: сохранённое в localStorage → системная тема → 'light'.
    const [theme, setTheme] = useState<'light' | 'dark'>(() => {
        const saved = localStorage.getItem('theme');
        if (saved === 'light' || saved === 'dark') return saved;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    });

    // Применяем класс `dark` к корневому элементу при изменении темы.
    useEffect(() => {
        const root = document.documentElement;
        if (theme === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }
        localStorage.setItem('theme', theme);
    }, [theme]);

    // Переключение темы.
    const toggleTheme = () => {
        setTheme(prev => (prev === 'light' ? 'dark' : 'light'));
    };

    // Данные пользователя и функции аутентификации.
    const { user, isAdmin, isTcx, logout, loading } = useAuth();

    // Пока Keycloak не инициализирован — показываем экран загрузки.
    // Это единственный момент, когда приложение не рендерит роутер;
    // после этого пользователь уже либо аутентифицирован, либо
    // находится в процессе редиректа на Keycloak login.
    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
                <div className="text-gray-700 dark:text-gray-300">Загрузка...</div>
            </div>
        );
    }

    // Метка и цвет роли для отображения в правом верхнем углу.
    const roleLabel = isAdmin ? 'Админ' : isTcx ? 'TCX' : 'Пользователь';
    const roleColor = isAdmin
        ? 'text-purple-600 dark:text-purple-400'
        : isTcx
            ? 'text-green-600 dark:text-green-400'
            : 'text-gray-700 dark:text-gray-200';

    return (
        <BrowserRouter>
            {/* Глобальный контейнер для уведомлений (тостов) */}
            <Toaster
                position="top-right"
                toastOptions={{
                    duration: 4000,
                    style: {
                        background: theme === 'dark' ? '#1f2937' : '#fff',
                        color: theme === 'dark' ? '#f3f4f6' : '#1f2937',
                    },
                }}
            />

            {/* Панель кнопок в правом верхнем углу: пользователь + тема + logout */}
            <div className="fixed top-4 right-4 z-50 flex items-center gap-2">
                {/* Информация о пользователе */}
                <div className="flex items-center gap-2 px-3 py-2 rounded-full bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm shadow-lg border border-gray-200 dark:border-gray-700">
                    <FiUser className={roleColor} />
                    <span className={`text-xs font-medium ${roleColor}`}>
                        {user?.firstName} {user?.lastName} ({roleLabel})
                    </span>
                </div>

                {/* Кнопка переключения темы */}
                <button
                    onClick={toggleTheme}
                    className="p-2 rounded-full bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm shadow-lg border border-gray-200 dark:border-gray-700 transition-all hover:scale-110"
                    aria-label="Переключить тему"
                >
                    {theme === 'light' ? (
                        <FiMoon className="w-5 h-5 text-gray-700 dark:text-gray-200" />
                    ) : (
                        <FiSun className="w-5 h-5 text-yellow-500" />
                    )}
                </button>

                {/* Кнопка выхода — вызывает Keycloak logout */}
                <button
                    onClick={logout}
                    className="p-2 rounded-full bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm shadow-lg border border-gray-200 dark:border-gray-700 transition-all hover:scale-110"
                    aria-label="Выйти"
                    title="Выйти из системы"
                >
                    <FiLogOut className="w-5 h-5 text-red-500" />
                </button>
            </div>

            {/* Основное приложение */}
            <Layout>
                <Routes>
                    {/* Основная страница контроля влажности */}
                    <Route path="/humidity" element={<HumidityPage />} />

                    {/* Детали машины */}
                    <Route path="/humidity/vehicles/:id" element={<VehicleDetailsPage />} />

                    {/* Корень → редирект на /humidity */}
                    <Route path="/" element={<Navigate to="/humidity" replace />} />

                    {/* Любой другой путь → редирект на /humidity.
                        Защищает от 404 и опечаток в URL. */}
                    <Route path="*" element={<Navigate to="/humidity" replace />} />
                </Routes>
            </Layout>
        </BrowserRouter>
    );
};

/**
 * Обёртка с AuthProvider — здесь и только здесь создаётся контекст.
 */
function App() {
    return (
        <AuthProvider>
            <AppInner />
        </AuthProvider>
    );
}

export default App;