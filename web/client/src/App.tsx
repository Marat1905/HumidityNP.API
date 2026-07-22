/**
 * Главный компонент приложения.
 * Управляет темой и маршрутизацией.
 */
import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { FiSun, FiMoon } from 'react-icons/fi';
import Layout from './components/Layout';
import VehiclesPage from './pages/VehiclesPage';
import VehicleDetailsPage from './pages/VehicleDetailsPage';
import './index.css';

function App() {
    const [theme, setTheme] = useState<'light' | 'dark'>(() => {
        const saved = localStorage.getItem('theme');
        if (saved === 'light' || saved === 'dark') return saved;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    });

    useEffect(() => {
        const root = document.documentElement;
        if (theme === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }
        localStorage.setItem('theme', theme);
    }, [theme]);

    const toggleTheme = () => {
        setTheme(prev => (prev === 'light' ? 'dark' : 'light'));
    };

    return (
        <BrowserRouter>
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
            <button
                onClick={toggleTheme}
                className="fixed top-4 right-4 z-50 p-2 rounded-full bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm shadow-lg border border-gray-200 dark:border-gray-700 transition-all hover:scale-110"
                aria-label="Переключить тему"
            >
                {theme === 'light' ? (
                    <FiMoon className="w-5 h-5 text-gray-700 dark:text-gray-200" />
                ) : (
                    <FiSun className="w-5 h-5 text-yellow-500" />
                )}
            </button>

            <Layout>
                <Routes>
                    <Route path="/" element={<VehiclesPage />} />
                    <Route path="/vehicles/:id" element={<VehicleDetailsPage />} />
                </Routes>
            </Layout>
        </BrowserRouter>
    );
}

export default App;