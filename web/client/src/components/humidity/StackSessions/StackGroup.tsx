import React, { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { ChevronDown, ChevronRight, Layers, Droplet, Activity, Truck, Package, Weight } from 'lucide-react';
import type { StackSessionDto } from '../../../types/humidity';
import StackSessionCard from './StackSessionCard';

interface StackGroupProps {
    stackNumber: string;
    sessions: StackSessionDto[];
    /** По умолчанию группа свёрнута. */
    defaultExpanded?: boolean;
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
 * Секция одного штабеля — аккордеон со списком сессий.
 *
 * В свёрнутом виде показывает:
 *  - номер штабеля;
 *  - количество сессий;
 *  - суммарное число замеров и машин;
 *  - среднее количество тюков за сессию;
 *  - диапазон дат (от первой до последней сессии);
 *  - среднюю влажность по штабелю.
 *
 * В развёрнутом виде — список карточек сессий.
 *
 * Сессии отображаются по УБЫВАНИЮ sessionId: последняя сессия — сверху,
 * самая ранняя — снизу. Это совпадает с порядком, который отдаёт сервер
 * (ORDER BY ... session_id DESC), но компонент всё равно сортирует сам —
 * защитно, чтобы UI не зависел от контракта API.
 *
 * Диапазон дат в шапке группы считается явно через min/max, чтобы не
 * зависеть от порядка сортировки массива.
 *
 * Тюки:
 *  - «Среднее за сессию» = сумма totalBales / количество сессий (только если
 *    есть данные хотя бы у одной сессии; иначе «—»).
 */
const StackGroup: React.FC<StackGroupProps> = ({
    stackNumber,
    sessions,
    defaultExpanded = false,
}) => {
    const [expanded, setExpanded] = useState(defaultExpanded);

    // Сессии — по убыванию sessionId (новые сверху).
    const sortedSessions = [...sessions].sort((a, b) => b.sessionId - a.sessionId);

    const totalMeasurements = sortedSessions.reduce((acc, s) => acc + s.measurementsCount, 0);
    const totalVehicles = sortedSessions.reduce((acc, s) => acc + s.vehiclesCount, 0);

    // Суммарные тюки по штабелю. Складываем только не-null значения:
    // null означает «нет данных», 0 — «ноль тюков». Если ни одной сессии
    // с данными нет, итог будет null (а не 0) — и клиент увидит «—».
    const sessionsWithBales = sortedSessions.filter(s => s.totalBales !== null);
    const totalBales = sessionsWithBales.length > 0
        ? sessionsWithBales.reduce((acc, s) => acc + (s.totalBales ?? 0), 0)
        : null;

    // Среднее количество тюков за сессию.
    // Считаем только по сессиям, где данные вообще есть — иначе
    // знаменатель раздувается «пустыми» сессиями и среднее не отражает реальность.
    const averageBalesPerSession = (totalBales !== null && sessionsWithBales.length > 0)
        ? totalBales / sessionsWithBales.length
        : null;

    // Суммарный вес по штабелю.
    const sessionsWithWeight = sortedSessions.filter(s => s.totalWeightKg !== null);
    const totalWeight = sessionsWithWeight.length > 0
        ? sessionsWithWeight.reduce((acc, s) => acc + (s.totalWeightKg ?? 0), 0)
        : null;

    // Диапазон дат считаем явно через min/max, чтобы он не зависел от порядка
    // отображения сессий. Если сессий нет — обе переменные undefined.
    const firstStart = sortedSessions.length > 0
        ? sortedSessions.reduce(
            (min, s) => (s.sessionStart < min ? s.sessionStart : min),
            sortedSessions[0].sessionStart
        )
        : undefined;

    const lastEnd = sortedSessions.length > 0
        ? sortedSessions.reduce(
            (max, s) => (s.sessionEnd > max ? s.sessionEnd : max),
            sortedSessions[0].sessionEnd
        )
        : undefined;

    // Взвешенная средняя влажность по штабелю: сессии с большим числом замеров
    // влияют сильнее. Это корректнее простого среднего по сессиям.
    const sessionsWithHumidity = sortedSessions.filter(s => s.averageHumidity !== null);
    const totalWeightForHumidity = sessionsWithHumidity.reduce((acc, s) => acc + s.measurementsCount, 0);
    const avgHumidity = totalWeightForHumidity > 0
        ? sessionsWithHumidity.reduce(
            (acc, s) => acc + (s.averageHumidity! * s.measurementsCount),
            0
        ) / totalWeightForHumidity
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

                {/* Сводные метрики по штабелю. На узких экранах прячем —
                    они дублируются в раскрытом виде. */}
                <div className="hidden lg:flex items-center gap-4 text-sm shrink-0">
                    <div className="flex items-center gap-1.5 text-gray-700 dark:text-gray-300" title="Замеров всего">
                        <Activity className="w-4 h-4 text-indigo-500" />
                        <span className="font-semibold">{totalMeasurements}</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-gray-700 dark:text-gray-300" title="Машин всего">
                        <Truck className="w-4 h-4 text-blue-500" />
                        <span className="font-semibold">{totalVehicles}</span>
                    </div>
                    
                    {/* Заменено «Всего тюков» и «Среднее на машину» на «Среднее за сессию» */}
                    <div className="flex items-center gap-1.5 text-gray-700 dark:text-gray-300" title="Среднее количество тюков за сессию">
                        <Package className="w-4 h-4 text-purple-500" />
                        <span className="font-semibold">
                            {averageBalesPerSession !== null
                                ? averageBalesPerSession.toFixed(1)
                                : '—'}
                        </span>
                    </div>

                    <div className={`flex items-center gap-1.5 font-semibold ${getHumidityColor(avgHumidity)}`} title="Средняя влажность по штабелю">
                        <Droplet className="w-4 h-4" />
                        <span>{avgHumidity !== null ? avgHumidity.toFixed(1) + '%' : '—'}</span>
                    </div>
                </div>
            </button>

            {expanded && (
                <div className="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-gray-900/50 space-y-3">
                    {/* Мини-сводка по тюкам в раскрытом виде — полезна на мобильных,
                        где верхние метрики скрыты (hidden lg:flex). */}
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-600 dark:text-gray-400">
                        <span className="inline-flex items-center gap-1.5">
                            <Package className="w-3.5 h-3.5 text-red-500" />
                            Порванных:{' '}
                            <span className="font-semibold text-gray-900 dark:text-white">
                                {formatBales(
                                    sessionsWithBales.length > 0
                                        ? sortedSessions
                                            .filter(s => s.totalDamagedBales !== null)
                                            .reduce((acc, s) => acc + (s.totalDamagedBales ?? 0), 0)
                                        : null
                                )}
                            </span>
                        </span>
                        <span className="inline-flex items-center gap-1.5">
                            <Weight className="w-3.5 h-3.5 text-green-500" />
                            Вес: <span className="font-semibold text-gray-900 dark:text-white">{formatWeight(totalWeight)}</span>
                        </span>
                        
                        {/* Добавлено «Среднее тюков за сессию» */}
                        <span className="inline-flex items-center gap-1.5">
                            <Package className="w-3.5 h-3.5 text-purple-500" />
                            Среднее тюков за сессию:{' '}
                            <span className="font-semibold text-gray-900 dark:text-white">
                                {averageBalesPerSession !== null ? averageBalesPerSession.toFixed(1) : '—'}
                            </span>
                        </span>
                    </div>

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