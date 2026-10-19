import React, { useMemo } from 'react';
import type { StackSessionDto, StackSessionsStatsDto } from '../../../types/humidity';
import {
    Package, Layers, Activity, Clock, Droplet, Info, Truck, Weight,
} from 'lucide-react';

interface StackSessionsSummaryProps {
    sessions: StackSessionDto[];
    /**
     * Сводная статистика по штабелям, полученная с сервера.
     * Может быть null, пока данные не загружены, или если запрос статистики упал.
     */
    stats: StackSessionsStatsDto | null;
}

/**
 * Форматирует число тюков: null → «—», иначе с разделителями разрядов.
 */
const formatBales = (value: number | null): string => {
    if (value === null || value === undefined) return '—';
    return value.toLocaleString('ru-RU');
};

/**
 * Форматирует вес (кг) с одним знаком после запятой.
 */
const formatWeight = (value: number | null): string => {
    if (value === null || value === undefined) return '—';
    return `${value.toFixed(1)} кг`;
};

/**
 * Сводная статистика по всем сессиям штабелей + пояснение расхождения
 * с «Отчётом за период».
 *
 * Плитки (штабелей, сессий, замеров, средняя влажность, средняя длительность,
 * всего тюков, среднее на машину) считаются на клиенте из уже загруженных
 * сессий — так быстрее и не требует дополнительного запроса.
 *
 * Число «Всего замеров за период» и «Без штабеля» берётся из stats — оно
 * считается на сервере по «сырым» замерам и объясняет, почему в отчёте
 * по штабелям замеров меньше, чем в «Отчёте за период».
 */
const StackSessionsSummary: React.FC<StackSessionsSummaryProps> = ({ sessions, stats }) => {
    const computed = useMemo(() => {
        if (!sessions.length) return null;

        const uniqueStacks = new Set(sessions.map(s => s.stackNumber)).size;
        const totalMeasurements = sessions.reduce((acc, s) => acc + s.measurementsCount, 0);
        const totalVehicles = sessions.reduce((acc, s) => acc + s.vehiclesCount, 0);

        // Взвешенная средняя влажность по всем сессиям: суммируем avg * weight
        // и делим на общий вес (число замеров). Так сессии с большим числом
        // замеров влияют сильнее — это правильнее простого среднего.
        const sessionsWithHumidity = sessions.filter(s => s.averageHumidity !== null);
        const totalWeightForHumidity = sessionsWithHumidity.reduce((acc, s) => acc + s.measurementsCount, 0);
        const weightedAvg = totalWeightForHumidity > 0
            ? sessionsWithHumidity.reduce(
                (acc, s) => acc + (s.averageHumidity! * s.measurementsCount),
                0
            ) / totalWeightForHumidity
            : null;

        // Длительность в DTO — строка «чч:мм:сс» (чч может быть > 24).
        const parseDuration = (dur: string): number => {
            const parts = dur.split(':').map(Number);
            if (parts.length !== 3 || parts.some(isNaN)) return 0;
            return parts[0] * 3600 + parts[1] * 60 + parts[2];
        };

        const totalDurationSec = sessions.reduce((acc, s) => acc + parseDuration(s.duration), 0);
        const avgDurationSec = totalDurationSec / sessions.length;
        const avgDurationH = Math.floor(avgDurationSec / 3600);
        const avgDurationM = Math.floor((avgDurationSec % 3600) / 60);

        // Суммарные тюки и вес по всем сессиям. Складываем только не-null,
        // чтобы null (нет данных) не превращался в 0. Если ни одной сессии
        // с данными нет — итог тоже null.
        const sessionsWithBales = sessions.filter(s => s.totalBales !== null);
        const totalBales = sessionsWithBales.length > 0
            ? sessionsWithBales.reduce((acc, s) => acc + (s.totalBales ?? 0), 0)
            : null;

        const sessionsWithWeight = sessions.filter(s => s.totalWeightKg !== null);
        const totalWeight = sessionsWithWeight.length > 0
            ? sessionsWithWeight.reduce((acc, s) => acc + (s.totalWeightKg ?? 0), 0)
            : null;

        // Среднее количество тюков на машину в отчёте. Знаменатель — число
        // машин только тех сессий, где данные вообще есть. Иначе «пустые»
        // машины раздували бы знаменатель и занижали среднее.
        const vehiclesWithBales = sessionsWithBales.reduce((acc, s) => acc + s.vehiclesCount, 0);
        const avgBalesPerVehicle = (totalBales !== null && vehiclesWithBales > 0)
            ? totalBales / vehiclesWithBales
            : null;

        return {
            uniqueStacks,
            totalSessions: sessions.length,
            totalMeasurements,
            totalVehicles,
            weightedAvg,
            avgDurationH,
            avgDurationM,
            totalBales,
            totalWeight,
            avgBalesPerVehicle,
        };
    }, [sessions]);

    if (!computed) return null;

    // Разница между «всего замеров» (из stats) и «замеров в сессиях» (из computed).
    // Если stats недоступен — показываем только computed.
    const measurementsDelta = stats
        ? stats.totalMeasurements - computed.totalMeasurements
        : 0;

    return (
        <div className="space-y-3 mb-6">
            {/* Плитки со сводкой. Сетка на 6 колонок в широком режиме —
                добавлены плитки «Тюки» и «Среднее на машину». */}
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
                <div className="bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-900/20 dark:to-purple-800/20 rounded-xl p-3 shadow-sm border border-purple-200 dark:border-purple-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-purple-600 dark:text-purple-400 uppercase tracking-wider">Штабелей</div>
                            <div className="text-2xl font-bold text-purple-700 dark:text-purple-300">{computed.uniqueStacks}</div>
                        </div>
                        <Layers className="w-7 h-7 text-purple-500 dark:text-purple-400 opacity-80" />
                    </div>
                </div>

                <div className="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-800/20 rounded-xl p-3 shadow-sm border border-blue-200 dark:border-blue-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-blue-600 dark:text-blue-400 uppercase tracking-wider">Сессий</div>
                            <div className="text-2xl font-bold text-blue-700 dark:text-blue-300">{computed.totalSessions}</div>
                        </div>
                        <Package className="w-7 h-7 text-blue-500 dark:text-blue-400 opacity-80" />
                    </div>
                </div>

                <div className="bg-gradient-to-br from-indigo-50 to-indigo-100 dark:from-indigo-900/20 dark:to-indigo-800/20 rounded-xl p-3 shadow-sm border border-indigo-200 dark:border-indigo-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-indigo-600 dark:text-indigo-400 uppercase tracking-wider">Замеров</div>
                            <div className="text-2xl font-bold text-indigo-700 dark:text-indigo-300">{computed.totalMeasurements}</div>
                            {stats && measurementsDelta > 0 && (
                                <div className="text-[10px] text-indigo-500 dark:text-indigo-400 mt-0.5">
                                    из {stats.totalMeasurements} всего
                                </div>
                            )}
                        </div>
                        <Activity className="w-7 h-7 text-indigo-500 dark:text-indigo-400 opacity-80" />
                    </div>
                </div>

                <div className="bg-gradient-to-br from-emerald-50 to-emerald-100 dark:from-emerald-900/20 dark:to-emerald-800/20 rounded-xl p-3 shadow-sm border border-emerald-200 dark:border-emerald-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-emerald-600 dark:text-emerald-400 uppercase tracking-wider">Средняя влажность</div>
                            <div className="text-2xl font-bold text-emerald-700 dark:text-emerald-300">
                                {computed.weightedAvg !== null ? computed.weightedAvg.toFixed(1) + '%' : '—'}
                            </div>
                        </div>
                        <Droplet className="w-7 h-7 text-emerald-500 dark:text-emerald-400 opacity-80" />
                    </div>
                </div>

                <div className="bg-gradient-to-br from-amber-50 to-amber-100 dark:from-amber-900/20 dark:to-amber-800/20 rounded-xl p-3 shadow-sm border border-amber-200 dark:border-amber-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-amber-600 dark:text-amber-400 uppercase tracking-wider">Средняя длительность</div>
                            <div className="text-xl font-bold text-amber-700 dark:text-amber-300 font-mono">
                                {String(computed.avgDurationH).padStart(2, '0')}:{String(computed.avgDurationM).padStart(2, '0')}
                            </div>
                        </div>
                        <Clock className="w-7 h-7 text-amber-500 dark:text-amber-400 opacity-80" />
                    </div>
                </div>

                {/* НОВАЯ ПЛИТКА: всего тюков по всем сессиям. */}
                <div className="bg-gradient-to-br from-violet-50 to-violet-100 dark:from-violet-900/20 dark:to-violet-800/20 rounded-xl p-3 shadow-sm border border-violet-200 dark:border-violet-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-[10px] font-medium text-violet-600 dark:text-violet-400 uppercase tracking-wider">Тюков всего</div>
                            <div className="text-2xl font-bold text-violet-700 dark:text-violet-300">
                                {formatBales(computed.totalBales)}
                            </div>
                            <div className="text-[10px] text-violet-500 dark:text-violet-400 mt-0.5">
                                {computed.avgBalesPerVehicle !== null
                                    ? `≈ ${computed.avgBalesPerVehicle.toFixed(1)} на машину`
                                    : 'нет данных'}
                            </div>
                        </div>
                        <Weight className="w-7 h-7 text-violet-500 dark:text-violet-400 opacity-80" />
                    </div>
                </div>
            </div>

            {/* Вторая строка сводки: суммарный вес и средние. Отдельной полосой,
                чтобы не перегружать основную сетку плиток. */}
            <div className="flex flex-wrap items-center gap-x-6 gap-y-2 px-3 py-2 rounded-lg bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 text-xs text-gray-600 dark:text-gray-400">
                <span className="inline-flex items-center gap-1.5">
                    <Truck className="w-3.5 h-3.5 text-blue-500" />
                    Машин в сессиях: <span className="font-semibold text-gray-900 dark:text-white">{computed.totalVehicles}</span>
                </span>
                <span className="inline-flex items-center gap-1.5">
                    <Weight className="w-3.5 h-3.5 text-green-500" />
                    Общий вес: <span className="font-semibold text-gray-900 dark:text-white">{formatWeight(computed.totalWeight)}</span>
                </span>
                <span className="inline-flex items-center gap-1.5">
                    <Package className="w-3.5 h-3.5 text-violet-500" />
                    Среднее тюков на машину:{' '}
                    <span className="font-semibold text-gray-900 dark:text-white">
                        {computed.avgBalesPerVehicle !== null ? computed.avgBalesPerVehicle.toFixed(1) : '—'}
                    </span>
                </span>
            </div>

            {/* Пояснение про расхождение с «Отчётом за период» */}
            {stats && (stats.measurementsWithoutStack > 0 || stats.vehiclesWithoutStack > 0) && (
                <div className="flex items-start gap-3 p-3 rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-sm">
                    <Info className="w-5 h-5 text-blue-600 dark:text-blue-400 shrink-0 mt-0.5" />
                    <div className="flex-1">
                        <div className="text-blue-800 dark:text-blue-300 mt-0.5">
                            В отчёт по штабелям попадают только замеры машин с заполненным номером штабеля.
                            За период <span className="font-semibold">всего {stats.totalMeasurements}</span> замеров,
                            из них <span className="font-semibold text-emerald-700 dark:text-emerald-400">{stats.measurementsWithStack}</span> — с указанным штабелем,{' '}
                            <span className="font-semibold text-amber-700 dark:text-amber-400">{stats.measurementsWithoutStack}</span> — без штабеля
                            (они в отчёт не попали).
                        </div>
                        <div className="text-xs text-blue-700 dark:text-blue-300 mt-1 flex flex-wrap gap-x-4 gap-y-1">
                            <span className="inline-flex items-center gap-1">
                                <Truck className="w-3.5 h-3.5" />
                                Машин: {stats.totalVehicles} всего,
                                {' '}<span className="text-emerald-700 dark:text-emerald-400">{stats.vehiclesWithStack} с штабелем</span>,
                                {' '}<span className="text-amber-700 dark:text-amber-400">{stats.vehiclesWithoutStack} без штабеля</span>
                            </span>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default StackSessionsSummary;