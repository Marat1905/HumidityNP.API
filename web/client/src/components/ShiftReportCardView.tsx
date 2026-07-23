// src/components/ShiftReportCardView.tsx
import React, { useState } from 'react';
import type { ShiftReportItem, ShiftSummaryStats } from '../hooks/useShiftReport';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import {
    Truck,
    Activity,
    Droplet,
    Zap,
    PenTool,
    TrendingUp,
    TrendingDown,
    ChevronDown,
    ChevronRight,
} from 'lucide-react';
import VehicleMeasurementsExpand from './VehicleMeasurementsExpand';

interface ShiftReportCardViewProps {
    items: ShiftReportItem[];
    summary: ShiftSummaryStats;
}

/**
 * Карточное представление отчёта по смене.
 * Каждая карточка — это одна машина, при клике на «Подробнее» раскрываются замеры.
 */
const ShiftReportCardView: React.FC<ShiftReportCardViewProps> = ({ items, summary }) => {
    const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

    const toggleExpand = (vehicleId: string) => {
        setExpandedIds((prev) => {
            const newSet = new Set(prev);
            if (newSet.has(vehicleId)) {
                newSet.delete(vehicleId);
            } else {
                newSet.add(vehicleId);
            }
            return newSet;
        });
    };

    if (items.length === 0) {
        return (
            <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                <Activity className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                <p>За выбранную смену нет замеров.</p>
            </div>
        );
    }

    return (
        <div>
            {/* Блок общей статистики (аналогичен табличному) */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                <div className="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-800/20 rounded-xl p-4 shadow-sm border border-blue-200 dark:border-blue-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-xs font-medium text-blue-600 dark:text-blue-400 uppercase tracking-wider">
                                Машин
                            </div>
                            <div className="text-3xl font-bold text-blue-700 dark:text-blue-300">
                                {summary.vehicleCount}
                            </div>
                        </div>
                        <Truck className="w-8 h-8 text-blue-500 dark:text-blue-400 opacity-80" />
                    </div>
                </div>
                <div className="bg-gradient-to-br from-indigo-50 to-indigo-100 dark:from-indigo-900/20 dark:to-indigo-800/20 rounded-xl p-4 shadow-sm border border-indigo-200 dark:border-indigo-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-xs font-medium text-indigo-600 dark:text-indigo-400 uppercase tracking-wider">
                                Всего замеров
                            </div>
                            <div className="text-3xl font-bold text-indigo-700 dark:text-indigo-300">
                                {summary.totalMeasurements}
                            </div>
                        </div>
                        <Activity className="w-8 h-8 text-indigo-500 dark:text-indigo-400 opacity-80" />
                    </div>
                </div>
                <div className="bg-gradient-to-br from-emerald-50 to-emerald-100 dark:from-emerald-900/20 dark:to-emerald-800/20 rounded-xl p-4 shadow-sm border border-emerald-200 dark:border-emerald-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-xs font-medium text-emerald-600 dark:text-emerald-400 uppercase tracking-wider">
                                Средняя влажность
                            </div>
                            <div className="text-3xl font-bold text-emerald-700 dark:text-emerald-300">
                                {summary.overallAverageHumidity !== null
                                    ? summary.overallAverageHumidity.toFixed(1) + '%'
                                    : '—'}
                            </div>
                        </div>
                        <Droplet className="w-8 h-8 text-emerald-500 dark:text-emerald-400 opacity-80" />
                    </div>
                </div>
                <div className="bg-gradient-to-br from-amber-50 to-amber-100 dark:from-amber-900/20 dark:to-amber-800/20 rounded-xl p-4 shadow-sm border border-amber-200 dark:border-amber-800">
                    <div className="flex items-center justify-between">
                        <div>
                            <div className="text-xs font-medium text-amber-600 dark:text-amber-400 uppercase tracking-wider">
                                Авто / Ручные
                            </div>
                            <div className="text-3xl font-bold text-amber-700 dark:text-amber-300 flex items-baseline gap-1">
                                <span>{summary.totalAutoCount}</span>
                                <span className="text-lg font-normal text-amber-500 dark:text-amber-400">/</span>
                                <span>{summary.totalManualCount}</span>
                            </div>
                        </div>
                        <div className="flex -space-x-1">
                            <Zap className="w-8 h-8 text-blue-500 dark:text-blue-400 opacity-80" />
                            <PenTool className="w-8 h-8 text-orange-500 dark:text-orange-400 opacity-80" />
                        </div>
                    </div>
                </div>
            </div>

            {/* Дополнительная статистика: минимум / максимум */}
            <div className="flex flex-wrap items-center gap-4 mb-6 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 text-sm">
                <div className="flex items-center gap-1.5">
                    <TrendingDown className="w-4 h-4 text-red-500" />
                    <span className="text-gray-600 dark:text-gray-400">Мин:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                        {summary.overallMinHumidity !== null
                            ? summary.overallMinHumidity.toFixed(1) + '%'
                            : '—'}
                    </span>
                </div>
                <div className="flex items-center gap-1.5">
                    <TrendingUp className="w-4 h-4 text-green-500" />
                    <span className="text-gray-600 dark:text-gray-400">Макс:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                        {summary.overallMaxHumidity !== null
                            ? summary.overallMaxHumidity.toFixed(1) + '%'
                            : '—'}
                    </span>
                </div>
                <div className="flex-1 h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div
                        className="h-full bg-gradient-to-r from-red-400 via-yellow-400 to-green-400 transition-all duration-500"
                        style={{
                            width: summary.overallMinHumidity !== null && summary.overallMaxHumidity !== null
                                ? `${((summary.overallMaxHumidity - summary.overallMinHumidity) / 100) * 100}%`
                                : '0%',
                            marginLeft: summary.overallMinHumidity !== null
                                ? `${(summary.overallMinHumidity / 100) * 100}%`
                                : '0%',
                        }}
                    />
                </div>
                <div className="text-xs text-gray-400 dark:text-gray-500">Диапазон влажности</div>
            </div>

            {/* Список карточек */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {items.map((item) => {
                    const isExpanded = expandedIds.has(item.vehicleId);
                    return (
                        <div
                            key={item.vehicleId}
                            className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 overflow-hidden transition-all"
                        >
                            {/* Основная карточка */}
                            <div className="p-4">
                                <div className="flex items-start justify-between">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2">
                                            <Truck className="w-5 h-5 text-blue-500 dark:text-blue-400" />
                                            <span className="font-mono text-sm font-semibold text-gray-900 dark:text-white">
                                                {item.vehicleId.slice(0, 8)}...
                                            </span>
                                        </div>
                                        <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                                            <div>
                                                <span className="text-gray-500 dark:text-gray-400">Замеров:</span>
                                                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                                                    {item.measurementsCount}
                                                </span>
                                            </div>
                                            <div>
                                                <span className="text-gray-500 dark:text-gray-400">Средняя:</span>
                                                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                                                    {item.averageHumidity !== null
                                                        ? item.averageHumidity.toFixed(1) + '%'
                                                        : '—'}
                                                </span>
                                            </div>
                                            <div>
                                                <span className="text-gray-500 dark:text-gray-400">Мин / Макс:</span>
                                                <span className="ml-1 font-medium">
                                                    {item.minHumidity !== null && item.maxHumidity !== null ? (
                                                        <>
                                                            <span className="text-blue-600 dark:text-blue-400">
                                                                {item.minHumidity.toFixed(1)}%
                                                            </span>
                                                            <span className="text-gray-400 mx-0.5">/</span>
                                                            <span className="text-red-600 dark:text-red-400">
                                                                {item.maxHumidity.toFixed(1)}%
                                                            </span>
                                                        </>
                                                    ) : (
                                                        '—'
                                                    )}
                                                </span>
                                            </div>
                                            <div>
                                                <span className="text-gray-500 dark:text-gray-400">Авто / Ручные:</span>
                                                <span className="ml-1 font-medium">
                                                    <span className="text-blue-600 dark:text-blue-400">
                                                        {item.autoCount}
                                                    </span>
                                                    <span className="text-gray-400 mx-0.5">/</span>
                                                    <span className="text-orange-600 dark:text-orange-400">
                                                        {item.manualCount}
                                                    </span>
                                                </span>
                                            </div>
                                        </div>
                                        <div className="mt-1 text-xs text-gray-400 dark:text-gray-500">
                                            Последний: {item.lastMeasurementTimestamp
                                                ? format(new Date(item.lastMeasurementTimestamp), 'dd MMM HH:mm', { locale: ru })
                                                : '—'}
                                        </div>
                                    </div>
                                    <button
                                        onClick={() => toggleExpand(item.vehicleId)}
                                        className="ml-2 p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 transition flex items-center gap-1 text-sm font-medium text-blue-600 dark:text-blue-400"
                                    >
                                        {isExpanded ? (
                                            <>
                                                <ChevronDown className="w-4 h-4" />
                                                Скрыть
                                            </>
                                        ) : (
                                            <>
                                                <ChevronRight className="w-4 h-4" />
                                                Подробнее
                                            </>
                                        )}
                                    </button>
                                </div>
                            </div>

                            {/* Раскрывающаяся область с замерами */}
                            {isExpanded && (
                                <div className="border-t border-gray-200 dark:border-gray-700 p-4 bg-gray-50 dark:bg-gray-900/50">
                                    <VehicleMeasurementsExpand vehicleId={item.vehicleId} compact={false} />
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

export default ShiftReportCardView;