import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Home, Truck, PlusCircle } from 'lucide-react';

interface LayoutProps {
    children: ReactNode;
}

export default function Layout({ children }: LayoutProps) {
    const location = useLocation();

    const isActive = (path: string) => location.pathname === path;

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
            {/* Верхняя панель */}
            <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 sticky top-0 z-10">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between items-center h-16">
                        <div className="flex items-center gap-2">
                            <Truck className="w-8 h-8 text-blue-600 dark:text-blue-400" />
                            <span className="text-xl font-bold text-gray-900 dark:text-white">
                                Контроль влажности макулатуры
                            </span>
                        </div>
                        <nav className="flex items-center gap-4">
                            <Link
                                to="/"
                                className={`flex items-center gap-1 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive('/')
                                        ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                                    }`}
                            >
                                <Home className="w-4 h-4" />
                                Машины
                            </Link>
                        </nav>
                    </div>
                </div>
            </header>

            {/* Основное содержимое */}
            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
                {children}
            </main>

            {/* Футер (опционально) */}
            <footer className="border-t border-gray-200 dark:border-gray-700 mt-8 py-4 text-center text-sm text-gray-500 dark:text-gray-400">
                © {new Date().getFullYear()} Humidity Control. Все права защищены.
            </footer>
        </div>
    );
}