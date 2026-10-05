import React, { useMemo } from 'react';
import type { StackSessionDto, StackSessionsStatsDto } from '../../../types/humidity';
import { Package, Layers, Activity, Clock, Droplet, Info, Truck, AlertTriangle } from 'lucide-react';

interface StackSessionsSummaryProps {
    sessions: StackSessionDto[];
    /**
     * Сводная статистика по штабелям, полученная с сервера.
     * Может быть null, пока данные не загружены, или если запрос статистики упал.
     */
    stats: StackSessionsStatsDto | null;
}

/**
 * Сводная статистика по всем сессиям штабелей + пояснение расхождения
 * с «Отчётом за период».
 *
 * Плитки (штабелей, сессий, замеров, средняя влажность, средняя длительность)
 * считаются на клиенте из уже загруженных сессий — так быстрее и не требует
 * дополнительного запроса.
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
        const totalWeight = sessionsWithHumidity.reduce((acc, s) => acc + s.measurementsCount, 0);
        const weightedAvg = totalWeight > 0
            ? sessionsWithHumidity.reduce(
                (acc, s) => acc + (s.averageHumidity! * s.measurementsCount),
                0
            ) / totalWeight
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

        return {
            uniqueStacks,
            totalSessions: sessions.length,
            totalMeasurements,
            totalVehicles,
            weightedAvg,
            avgDurationH,
            avgDurationM,
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
            {/* Плитки со сводкой */}
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
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
            </div>

            {/* Пояснение про расхождение с «Отчётом за период» */}
            {stats && (stats.measurementsWithoutStack > 0 || stats.vehiclesWithoutStack > 0) && (
                <div className="flex items-start gap-3 p-3 rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-sm">
                    <Info className="w-5 h-5 text-blue-600 dark:text-blue-400 shrink-0 mt-0.5" />
                    <div className="flex-1">
                        <div className="font-medium text-blue-900 dark:text-blue-200">
                            Почему цифры отличаются от «Отчёта за период»
                        </div>
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
                            <span className="inline-flex items-center gap-1">
                                <AlertTriangle className="w-3.5 h-3.5" />
                                Заполните StackNumber при разгрузке, чтобы данные попадали в отчёт по штабелям.
                            </span>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default StackSessionsSummary;