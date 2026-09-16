/**
 * Главный компонент приложения (humidity).
 * Управляет темой оформления (светлая/тёмная) и отображает глобальные уведомления.
 * Содержит кнопки в правом верхнем углу: профиль пользователя, выход и переключение темы.
 */
import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router';
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
    // Состояние темы: 'light' или 'dark'
    const [theme, setTheme] = useState<'light' | 'dark'>(() => {
        const saved = localStorage.getItem('theme');
        if (saved === 'light' || saved === 'dark') return saved;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    });

    // Применяем класс `dark` к корневому элементу при изменении темы
    useEffect(() => {
        const root = document.documentElement;
        if (theme === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }
        localStorage.setItem('theme', theme);
    }, [theme]);

    // Переключение темы
    const toggleTheme = () => {
        setTheme(prev => (prev === 'light' ? 'dark' : 'light'));
    };

    // Получаем реальные данные пользователя и функции управления сессией из Keycloak
    const { user, isAuthenticated, loading, login, logout } = useAuth();

    // Показываем индикатор загрузки, пока Keycloak инициализируется
    if (loading) {
        return (
            <div className="flex items-center justify-center h-screen bg-gray-100 dark:bg-gray-900">
                <div className="text-xl text-gray-700 dark:text-gray-200">Загрузка и проверка сессии...</div>
            </div>
        );
    }

    // Если пользователь не авторизован, предлагаем войти
    if (!isAuthenticated) {
        return (
            <div className="flex flex-col items-center justify-center h-screen bg-gray-100 dark:bg-gray-900 gap-4">
                <div className="text-xl text-gray-700 dark:text-gray-200">Для работы необходимо авторизоваться</div>
                <button
                    onClick={login}
                    className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors shadow-lg"
                >
                    Войти через Keycloak
                </button>
            </div>
        );
    }

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

            {/* Панель кнопок в правом верхнем углу: профиль + тема */}
            <div className="fixed top-4 right-4 z-50 flex items-center gap-2">
                {/* Информация о пользователе и кнопка выхода */}
                <div className="flex items-center gap-2 px-3 py-2 rounded-full bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm shadow-lg border border-gray-200 dark:border-gray-700">
                    <FiUser className="w-5 h-5 text-gray-700 dark:text-gray-200" />
                    <span className="text-xs font-medium text-gray-700 dark:text-gray-200">
                        {user?.firstName || user?.username}
                    </span>
                    <button
                        onClick={logout}
                        className="p-1 rounded-full hover:bg-red-100 dark:hover:bg-red-900/50 transition-colors"
                        aria-label="Выйти"
                        title="Выйти из аккаунта"
                    >
                        <FiLogOut className="w-4 h-4 text-red-600 dark:text-red-400" />
                    </button>
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
            </div>

            {/* Основное приложение */}
            <Layout>
                <Routes>
                    <Route path="/humidity" element={<HumidityPage />} />
                    <Route path="/humidity/vehicles/:id" element={<VehicleDetailsPage />} />
                    {/* Редирект на главную страницу по умолчанию */}
                    <Route path="/" element={<HumidityPage />} />
                </Routes>
            </Layout>
        </BrowserRouter>
    );
};

/** Обёртка с AuthProvider — здесь и только здесь создаётся контекст */
function App() {
    return (
        <AuthProvider>
            <AppInner />
        </AuthProvider>
    );
}

export default App;