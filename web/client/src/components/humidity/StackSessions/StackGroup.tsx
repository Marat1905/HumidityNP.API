import React, { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { ChevronDown, ChevronRight, Layers, Droplet, Activity, Truck } from 'lucide-react';
import type { StackSessionDto } from '../../../types/humidity';
import StackSessionCard from './StackSessionCard';

interface StackGroupProps {
    stackNumber: string;
    sessions: StackSessionDto[];
    /** По умолчанию группа свёрнута. */
    defaultExpanded?: boolean;
}

/**
 * Секция одного штабеля — аккордеон со списком сессий.
 *
 * В свёрнутом виде показывает:
 *  - номер штабеля;
 *  - количество сессий;
 *  - суммарное число замеров и машин;
 *  - диапазон дат (от первой до последней сессии);
 *  - среднюю влажность по штабелю.
 *
 * В развёрнутом виде — список карточек сессий.
 */
const StackGroup: React.FC<StackGroupProps> = ({
    stackNumber,
    sessions,
    defaultExpanded = false,
}) => {
    const [expanded, setExpanded] = useState(defaultExpanded);

    // Сортировку сессий делаем по sessionId (сервер уже отдаёт их по порядку,
    // но подстрахуемся на случай будущих изменений в SQL).
    const sortedSessions = [...sessions].sort((a, b) => a.sessionId - b.sessionId);

    const totalMeasurements = sortedSessions.reduce((acc, s) => acc + s.measurementsCount, 0);
    const totalVehicles = sortedSessions.reduce((acc, s) => acc + s.vehiclesCount, 0);

    const firstStart = sortedSessions[0]?.sessionStart;
    const lastEnd = sortedSessions[sortedSessions.length - 1]?.sessionEnd;

    // Взвешенная средняя влажность по штабелю: сессии с большим числом замеров
    // влияют сильнее. Это корректнее простого среднего по сессиям.
    const sessionsWithHumidity = sortedSessions.filter(s => s.averageHumidity !== null);
    const totalWeight = sessionsWithHumidity.reduce((acc, s) => acc + s.measurementsCount, 0);
    const avgHumidity = totalWeight > 0
        ? sessionsWithHumidity.reduce(
            (acc, s) => acc + (s.averageHumidity! * s.measurementsCount),
            0
        ) / totalWeight
        : null;

    const getHumidityColor = (value: number | null) => {
        if (value === null) return 'text-gray-400';
        if (value < 10) return 'text-green-600 dark:text-green-400';
        if (value < 15) return 'text-yellow-600 dark:text-yellow-400';
        return 'text-red-600 dark:text-red-400';
    };

    const formatRange = (fromIso: string, toIso: string): string => {
        const fromStr = format(new Date(fromIso), 'dd.MM.yyyy', { locale: ru });
        const toStr = format(new Date(toIso), 'dd.MM.yyyy', { locale: ru });
        return fromStr === toStr ? fromStr : `${fromStr} — ${toStr}`;
    };

    return (
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
            <button
                onClick={() => setExpanded(prev => !prev)}
                className="w-full flex items-center justify-between gap-4 p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition text-left"
            >
                <div className="flex items-center gap-3 min-w-0 flex-1">
                    <span className="text-gray-400 shrink-0">
                        {expanded ? <ChevronDown className="w-5 h-5" /> : <ChevronRight className="w-5 h-5" />}
                    </span>

                    <div className="p-2 bg-gradient-to-br from-purple-500 to-purple-600 rounded-lg shadow-sm shrink-0">
                        <Layers className="w-5 h-5 text-white" />
                    </div>

                    <div className="min-w-0">
                        <div className="text-lg font-bold text-gray-900 dark:text-white truncate">
                            Штабель {stackNumber}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 truncate">
                            {sortedSessions.length} {pluralizeSessions(sortedSessions.length)}
                            {firstStart && lastEnd && (
                                <>
                                    {' · '}
                                    {formatRange(firstStart, lastEnd)}
                                </>
                            )}
                        </div>
                    </div>
                </div>

                <div className="hidden md:flex items-center gap-4 text-sm shrink-0">
                    <div className="flex items-center gap-1.5 text-gray-700 dark:text-gray-300" title="Замеров">
                        <Activity className="w-4 h-4 text-indigo-500" />
                        <span className="font-semibold">{totalMeasurements}</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-gray-700 dark:text-gray-300" title="Машин">
                        <Truck className="w-4 h-4 text-blue-500" />
                        <span className="font-semibold">{totalVehicles}</span>
                    </div>
                    <div className={`flex items-center gap-1.5 font-semibold ${getHumidityColor(avgHumidity)}`} title="Средняя влажность">
                        <Droplet className="w-4 h-4" />
                        <span>{avgHumidity !== null ? avgHumidity.toFixed(1) + '%' : '—'}</span>
                    </div>
                </div>
            </button>

            {expanded && (
                <div className="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-gray-900/50 space-y-3">
                    {sortedSessions.map(s => (
                        <StackSessionCard
                            key={`${s.stackNumber}-${s.sessionId}`}
                            session={s}
                            totalInStack={sortedSessions.length}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};

/**
 * Склонение слова «сессия» для русского языка.
 */
function pluralizeSessions(n: number): string {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) return 'сессия';
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return 'сессии';
    return 'сессий';
}

export default StackGroup;