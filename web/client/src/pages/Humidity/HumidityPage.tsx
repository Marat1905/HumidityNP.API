import { useState } from 'react';
import { VehiclesPage, MeasurementsPage, ShiftReportsPage, ReportPeriodPage, SuppliersPage, TopSuppliersPage } from '../Humidity'
import { useBackendVersion } from '../../hooks/humidity';

/**
 * Страница контроля влажности макулатуры.
 * Содержит шесть вкладок:
 *  - Машины;
 *  - Все замеры;
 *  - Отчёты по сменам;
 *  - Отчёт за период;
 *  - Поставщики;
 *  - Топ поставщиков.
 *
 * Справа от вкладок отображается версия бэкенда (Humidity.API),
 * получаемая с эндпоинта /humidity/api/v1/version через хук useBackendVersion.
 */
export default function HumidityPage() {
    const [activeTab, setActiveTab] = useState<'vehicles' | 'measurements' | 'reports' | 'period' | 'suppliers' | 'top'>('vehicles');

    // Версия бэкенда (null, пока не загружена)
    const version = useBackendVersion();

    return (
        <div>
            {/* Контейнер с заголовком и вкладками.
                Версия бэкенда выровнена по правому краю той же строки, что и вкладки. */}
            <div className="border-b border-gray-200 dark:border-gray-700 mb-6">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                    Контроль влажности макулатуры
                </h1>
                <div className="flex items-center justify-between gap-4">
                    <nav className="flex gap-6 flex-wrap">
                        <button
                            onClick={() => setActiveTab('vehicles')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'vehicles'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Машины
                        </button>
                        <button
                            onClick={() => setActiveTab('measurements')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'measurements'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Все замеры
                        </button>
                        <button
                            onClick={() => setActiveTab('reports')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'reports'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Отчёты по сменам
                        </button>
                        <button
                            onClick={() => setActiveTab('period')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'period'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Отчёт за период
                        </button>
                        <button
                            onClick={() => setActiveTab('suppliers')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'suppliers'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Поставщики
                        </button>
                        <button
                            onClick={() => setActiveTab('top')}
                            className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'top'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                                }`}
                        >
                            Топ поставщиков
                        </button>
                    </nav>

                    {/* Версия бэкенда справа от вкладок.
                        Отображается только когда данные успешно загружены. */}
                    {version && (
                        <div
                            className="flex items-center gap-1.5 pb-3 text-xs font-mono text-gray-500 dark:text-gray-400"
                            title={`Версия бэкенда: ${version}`}
                        >
                            <svg className="w-3.5 h-3.5 text-blue-500 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01" />
                            </svg>
                            <span>v{version}</span>
                        </div>
                    )}
                </div>
            </div>

            {activeTab === 'vehicles' && <VehiclesPage />}
            {activeTab === 'measurements' && <MeasurementsPage />}
            {activeTab === 'reports' && <ShiftReportsPage />}
            {activeTab === 'period' && <ReportPeriodPage />}
            {activeTab === 'suppliers' && <SuppliersPage />}
            {activeTab === 'top' && <TopSuppliersPage />}
        </div>
    );
}