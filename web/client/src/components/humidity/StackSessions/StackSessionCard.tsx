import React, { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import {
    Clock, Activity, Truck, Users, Droplet,
    TrendingDown, TrendingUp, ChevronDown, ChevronRight,
    UserCircle2, Timer, Package, Weight,
} from 'lucide-react';
import type { StackSessionDto } from '../../../types/humidity';

interface StackSessionCardProps {
    session: StackSessionDto;
    /** Общее число сессий у штабеля — для подписи «Сессия N из M». */
    totalInStack?: number;
}

/**
 * Форматирует число тюков:
 *  - null → «—» (нет данных);
 *  - иначе — число с разделителями разрядов.
 */
const formatBales = (value: number | null): string => {
    if (value === null || value === undefined) return '—';
    return value.toLocaleString('ru-RU');
};

/**
 * Форматирует вес (кг) с одним знаком после запятой:
 *  - null → «—»;
 *  - иначе — число + « кг».
 */
const formatWeight = (value: number | null): string => {
    if (value === null || value === undefined) return '—';
    return `${value.toFixed(1)} кг`;
};

/**
 * Карточка одной сессии работы со штабелем.
 *
 * Особенности:
 *  - слева — цветная полоска-индикатор средней влажности
 *    (зелёный < 10%, жёлтый 10–15%, красный > 15%);
 *  - в шапке — номер сессии, временной диапазон, длительность;
 *  - в теле — колонки: метрики, влажность, тюки, поставщики;
 *  - список поставщиков сворачивается/разворачивается по клику,
 *    и в развёрнутом виде показывает по каждому поставщику количество тюков.
 *
 * Влажности и тюки nullable: null показывается как «—», а не как «0.00%»/«0».
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
    // Но для отображения тюков по каждому поставщику используем counterpartiesDetails.
    const counterpartiesList = session.counterparties
        ? session.counterparties.split('; ').filter(Boolean)
        : [];

    const visibleCounterparties = showAllCounterparties
        ? session.counterpartiesDetails
        : session.counterpartiesDetails.slice(0, 3);

    const hasMoreCounterparties = session.counterpartiesDetails.length > 3;

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

            {/* Тело карточки: четыре колонки */}
            <div className="p-4 grid grid-cols-1 md:grid-cols-4 gap-4">
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

                {/* Колонка 3: тюки */}
                <div className="space-y-2">
                    <div className="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">
                        Тюки
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Package className="w-4 h-4 text-indigo-500" />
                        <span className="text-gray-600 dark:text-gray-400">Всего:</span>
                        <span className="font-bold text-gray-900 dark:text-white">
                            {formatBales(session.totalBales)}
                        </span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Package className="w-4 h-4 text-red-500" />
                        <span className="text-gray-600 dark:text-gray-400">Порванных:</span>
                        <span className="font-medium text-red-600 dark:text-red-400">
                            {formatBales(session.totalDamagedBales)}
                        </span>
                    </div>
                    <div className="flex items-center gap-2 text-sm">
                        <Weight className="w-4 h-4 text-green-500" />
                        <span className="text-gray-600 dark:text-gray-400">Вес:</span>
                        <span className="font-medium text-green-700 dark:text-green-400">
                            {formatWeight(session.totalWeightKg)}
                        </span>
                    </div>
                </div>

                {/* Колонка 4: поставщики */}
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
                            {session.primaryCounterpartyBales !== null && (
                                <div className="text-xs text-gray-500 dark:text-gray-400">
                                    Тюков: <span className="font-medium text-gray-800 dark:text-gray-200">
                                        {formatBales(session.primaryCounterpartyBales)}
                                    </span>
                                </div>
                            )}
                        </div>
                    </div>

                    {session.counterpartiesDetails.length > 1 && (
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
                                        Показать всех ({session.counterpartiesDetails.length})
                                    </>
                                )}
                            </button>
                            <ul className="mt-2 space-y-1.5 text-xs text-gray-600 dark:text-gray-400">
                                {visibleCounterparties.map((cp, idx) => (
                                    <li key={`${cp.name}-${idx}`} className="flex flex-wrap items-center gap-1.5">
                                        <span className="w-1 h-1 rounded-full bg-gray-400" />
                                        <span className={idx === 0 ? 'font-medium text-gray-900 dark:text-white' : ''}>
                                            {cp.name}
                                        </span>
                                        {idx === 0 && (
                                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300 whitespace-nowrap">
                                                основной
                                            </span>
                                        )}
                                        <span className="text-[11px] text-gray-500 dark:text-gray-400">
                                            ({cp.vehiclesCount} {pluralizeVehicles(cp.vehiclesCount)}, {cp.bales !== null ? `${formatBales(cp.bales)} тюков` : 'тюки —'})
                                        </span>
                                    </li>
                                ))}
                                {!showAllCounterparties && hasMoreCounterparties && (
                                    <li className="text-gray-400 dark:text-gray-500 italic">
                                        и ещё {session.counterpartiesDetails.length - 3}…
                                    </li>
                                )}
                            </ul>
                        </div>
                    )}

                    {/* Если поставщик один, но у него есть детали — всё равно показываем
                        его в списке (без кнопки «Показать всех»). */}
                    {session.counterpartiesDetails.length === 1 && session.counterpartiesDetails[0].bales !== null && (
                        <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                            Тюков у поставщика: <span className="font-medium text-gray-800 dark:text-gray-200">
                                {formatBales(session.counterpartiesDetails[0].bales)}
                            </span>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

/**
 * Склонение слова «машина» для русского языка.
 */
function pluralizeVehicles(n: number): string {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) return 'машина';
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return 'машины';
    return 'машин';
}

export default StackSessionCard;