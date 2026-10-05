import React, { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import {
    Clock, Activity, Truck, Users, Droplet,
    TrendingDown, TrendingUp, ChevronDown, ChevronRight,
    UserCircle2, Timer,
} from 'lucide-react';
import type { StackSessionDto } from '../../../types/humidity';

interface StackSessionCardProps {
    session: StackSessionDto;
    /** Общее число сессий у штабеля — для подписи «Сессия N из M». */
    totalInStack?: number;
}

/**
 * Карточка одной сессии работы со штабелем.
 *
 * Особенности:
 *  - слева — цветная полоска-индикатор средней влажности
 *    (зелёный < 10%, жёлтый 10–15%, красный > 15%);
 *  - в шапке — номер сессии, временной диапазон, длительность;
 *  - в теле — три колонки: метрики, влажность, поставщики;
 *  - список поставщиков сворачивается/разворачивается по клику.
 *
 * Влажности nullable: null показывается как «—», а не как «0.00%».
 */
const StackSessionCard: React.FC<StackSessionCardProps> = ({ session, totalInStack }) => {
    const [showAllCounterparties, setShowAllCounterparties] = useState(false);

    const formatSessionDate = (iso: string) =>
        format(new Date(iso), 'dd.MM.yyyy HH:mm', { locale: ru });

    // Цвет текста средней влажности — те же пороги, что в VehicleCard.
    const getHumidityTextColor = (value: number | null) => {
        if (value === null) return 'text-gray-400';
        if (value < 10) return 'text-green-600 dark:text-green-400';
        if (value < 15) return 'text-yellow-600 dark:text-yellow-400';
        return 'text-red-600 dark:text-red-400';
    };

    // Цвет полоски-индикатора слева.
    const getHumidityBarColor = (value: number | null) => {
        if (value === null) return 'bg-gray-300 dark:bg-gray-600';
        if (value < 10) return 'bg-green-500';
        if (value < 15) return 'bg-yellow-500';
        return 'bg-red-500';
    };

    // Список поставщиков приходит строкой с разделителем «; ».
    const counterpartiesList = session.counterparties
        ? session.counterparties.split('; ').filter(Boolean)
        : [];

    const visibleCounterparties = showAllCounterparties
        ? counterpartiesList
        : counterpartiesList.slice(0, 3);

    const hasMoreCounterparties = counterpartiesList.length > 3;

    return (
        <div className="relative bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm hover:shadow-md transition-shadow overflow-hidden">
            {/* Цветная полоска слева — индикатор средней влажности */}
            <div className={`absolute left-0 top-0 bottom-0 w-1 ${getHumidityBarColor(session.averageHumidity)}`} />

            {/* Шапка карточки: номер сессии и время */}
            <div className="flex flex-wrap items-center justify-between gap-2 pl-5 pr-4 py-3 border-b border-gray-100 dark:border-gray-700">
                <div className="flex items-center gap-2">
                    <span className="inline-flex items-center justify-center min-w-[2.5rem] h-8 px-2 rounded-full bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 text-sm font-bold">
                        #{session.sessionId}
                    </span>
                    {totalInStack !== undefined && (
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                            из {totalInStack}
                        </span>
                    )}
                </div>
                <div className="flex flex-wrap items-center gap-3 text-xs text-gray-600 dark:text-gray-300">
                    <span className="inline-flex items-center gap-1">
                        <Clock className="w-3.5 h-3.5" />
                        {formatSessionDate(session.sessionStart)}
                    </span>
                    <span className="text-gray-400 dark:text-gray-500">→</span>
                    <span className="inline-flex items-center gap-1">
                        {formatSessionDate(session.sessionEnd)}
                    </span>
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-gray-100 dark:bg-gray-700 font-mono text-gray-700 dark:text-gray-300">
                        <Timer className="w-3 h-3" />
                        {session.duration}
                    </span>
                </div>
            </div>

            {/* Тело карточки: три колонки */}
            <div className="p-4 grid grid-cols-1 md:grid-cols-3 gap-4">
                {/* Колонка 1: метрики */}
                <div className="space-y-2">
                    <div className="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">
                        Метрики
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Activity className="w-4 h-4 text-indigo-500" />
                        <span className="text-gray-600 dark:text-gray-400">Замеров:</span>
                        <span className="font-bold text-gray-900 dark:text-white">{session.measurementsCount}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Truck className="w-4 h-4 text-blue-500" />
                        <span className="text-gray-600 dark:text-gray-400">Машин:</span>
                        <span className="font-bold text-gray-900 dark:text-white">{session.vehiclesCount}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Users className="w-4 h-4 text-purple-500" />
                        <span className="text-gray-600 dark:text-gray-400">Поставщиков:</span>
                        <span className="font-bold text-gray-900 dark:text-white">{session.counterpartyCount}</span>
                    </div>
                </div>

                {/* Колонка 2: влажность */}
                <div className="space-y-2">
                    <div className="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">
                        Влажность
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Droplet className="w-4 h-4 text-emerald-500" />
                        <span className="text-gray-600 dark:text-gray-400">Средняя:</span>
                        <span className={`font-bold ${getHumidityTextColor(session.averageHumidity)}`}>
                            {session.averageHumidity !== null
                                ? session.averageHumidity.toFixed(2) + '%'
                                : '—'}
                        </span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <TrendingDown className="w-4 h-4 text-blue-500" />
                        <span className="text-gray-600 dark:text-gray-400">Мин:</span>
                        <span className="font-medium text-blue-600 dark:text-blue-400">
                            {session.minHumidity !== null
                                ? session.minHumidity.toFixed(2) + '%'
                                : '—'}
                        </span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <TrendingUp className="w-4 h-4 text-red-500" />
                        <span className="text-gray-600 dark:text-gray-400">Макс:</span>
                        <span className="font-medium text-red-600 dark:text-red-400">
                            {session.maxHumidity !== null
                                ? session.maxHumidity.toFixed(2) + '%'
                                : '—'}
                        </span>
                    </div>
                </div>

                {/* Колонка 3: поставщики */}
                <div className="space-y-2">
                    <div className="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">
                        Поставщики
                    </div>
                    <div className="flex items-start gap-2 text-sm">
                        <UserCircle2 className="w-4 h-4 text-emerald-500 mt-0.5" />
                        <div className="min-w-0">
                            <div className="text-gray-600 dark:text-gray-400 text-xs">Основной:</div>
                            <div className="font-semibold text-gray-900 dark:text-white truncate">
                                {session.primaryCounterparty || '—'}
                            </div>
                        </div>
                    </div>

                    {counterpartiesList.length > 1 && (
                        <div className="mt-1">
                            <button
                                onClick={() => setShowAllCounterparties(prev => !prev)}
                                className="inline-flex items-center gap-1 text-xs font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition"
                            >
                                {showAllCounterparties ? (
                                    <>
                                        <ChevronDown className="w-3.5 h-3.5" />
                                        Свернуть список
                                    </>
                                ) : (
                                    <>
                                        <ChevronRight className="w-3.5 h-3.5" />
                                        Показать всех ({counterpartiesList.length})
                                    </>
                                )}
                            </button>
                            <ul className="mt-2 space-y-1 text-xs text-gray-600 dark:text-gray-400">
                                {visibleCounterparties.map((cp, idx) => (
                                    <li key={idx} className="flex items-center gap-1.5">
                                        <span className="w-1 h-1 rounded-full bg-gray-400" />
                                        <span className={idx === 0 ? 'font-medium text-gray-900 dark:text-white' : ''}>
                                            {cp}
                                        </span>
                                        {idx === 0 && (
                                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300 whitespace-nowrap">
                                                основной
                                            </span>
                                        )}
                                    </li>
                                ))}
                                {!showAllCounterparties && hasMoreCounterparties && (
                                    <li className="text-gray-400 dark:text-gray-500 italic">
                                        и ещё {counterpartiesList.length - 3}…
                                    </li>
                                )}
                            </ul>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default StackSessionCard;