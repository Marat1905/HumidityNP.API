import { Calendar, Activity, Thermometer, Droplet, BarChart, Zap, PenTool, Clock } from 'lucide-react';
import type { MeasurementStatisticsDto } from '../types';

interface MeasurementStatisticsProps {
    stats: MeasurementStatisticsDto;
}

/**
 * Компонент для отображения статистики по замерам влажности.
 * Показывает количество замеров, среднюю, минимальную, максимальную влажность,
 * а также разбивку по источникам (авто/ручные) и дату последнего замера.
 * Использует цветовые акценты и иконки для наглядности.
 */
export default function MeasurementStatistics({ stats }: MeasurementStatisticsProps) {
    if (!stats || stats.count === 0) {
        return (
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6 text-center text-gray-500 dark:text-gray-400 border border-gray-200 dark:border-gray-700">
                <BarChart className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                <p>Нет замеров для отображения статистики.</p>
            </div>
        );
    }

    // Вычисляем процентное соотношение авто/ручных замеров для прогресс-бара
    const total = stats.autoCount + stats.manualCount;
    const autoPercent = total > 0 ? (stats.autoCount / total) * 100 : 0;

    // Форматирование даты последнего замера
    const lastDate = stats.lastMeasurementTimestamp
        ? new Date(stats.lastMeasurementTimestamp).toLocaleString('ru-RU', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        })
        : '—';

    return (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 mb-6">
            {/* Карточка: Всего замеров */}
            <div className="flex items-center gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                <div className="p-2 bg-blue-100 dark:bg-blue-800 rounded-full">
                    <Activity className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Всего замеров</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">{stats.count}</div>
                </div>
            </div>

            {/* Карточка: Средняя влажность */}
            <div className="flex items-center gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                <div className="p-2 bg-green-100 dark:bg-green-800 rounded-full">
                    <Droplet className="w-5 h-5 text-green-600 dark:text-green-400" />
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Средняя влажность</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">
                        {stats.average !== null ? stats.average.toFixed(1) + '%' : '—'}
                    </div>
                </div>
            </div>

            {/* Карточка: Минимум / Максимум */}
            <div className="flex items-center gap-3 p-3 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                <div className="p-2 bg-purple-100 dark:bg-purple-800 rounded-full">
                    <Thermometer className="w-5 h-5 text-purple-600 dark:text-purple-400" />
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Мин / Макс</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">
                        {stats.min !== null && stats.max !== null
                            ? `${stats.min.toFixed(1)}% / ${stats.max.toFixed(1)}%`
                            : '—'}
                    </div>
                </div>
            </div>

            {/* Карточка: Источники + последний замер */}
            <div className="flex flex-col gap-2 p-3 bg-orange-50 dark:bg-orange-900/20 rounded-lg">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                        <div className="p-1 bg-orange-100 dark:bg-orange-800 rounded-full">
                            <Zap className="w-4 h-4 text-orange-600 dark:text-orange-400" />
                        </div>
                        <span className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Источники</span>
                    </div>
                    <span className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                        {stats.autoCount} / {stats.manualCount}
                    </span>
                </div>
                {/* Прогресс-бар */}
                <div className="w-full h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div
                        className="h-full bg-blue-500 dark:bg-blue-400 transition-all duration-300"
                        style={{ width: `${autoPercent}%` }}
                    />
                </div>
                <div className="flex justify-between text-[10px] text-gray-400 dark:text-gray-500">
                    <span>Авто</span>
                    <span>Ручные</span>
                </div>
                <div className="flex items-center gap-1 mt-1 text-xs text-gray-500 dark:text-gray-400">
                    <Clock className="w-3 h-3" />
                    <span>Последний: {lastDate}</span>
                </div>
            </div>
        </div>
    );
}