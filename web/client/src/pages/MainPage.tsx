// src/pages/MainPage.tsx
import { useState } from 'react';
import VehiclesPage from './VehiclesPage';
import MeasurementsPage from './MeasurementsPage';
import ShiftReportsPage from './ShiftReportsPage';
import ReportPeriodPage from './ReportPeriodPage'; // <-- новый импорт

export default function MainPage() {
    const [activeTab, setActiveTab] = useState<'vehicles' | 'measurements' | 'reports' | 'period'>('vehicles');

    return (
        <div>
            <div className="border-b border-gray-200 dark:border-gray-700 mb-6">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                    Контроль влажности макулатуры
                </h1>
                <nav className="flex gap-6">
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
                    {/* Новая вкладка */}
                    <button
                        onClick={() => setActiveTab('period')}
                        className={`pb-3 px-1 text-sm font-medium transition-colors ${activeTab === 'period'
                                ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400'
                                : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                            }`}
                    >
                        Отчёт за период
                    </button>
                </nav>
            </div>

            {activeTab === 'vehicles' && <VehiclesPage />}
            {activeTab === 'measurements' && <MeasurementsPage />}
            {activeTab === 'reports' && <ShiftReportsPage />}
            {activeTab === 'period' && <ReportPeriodPage />}
        </div>
    );
}