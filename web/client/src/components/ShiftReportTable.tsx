// src/components/ShiftReportTable.tsx
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

interface ShiftReportTableProps {
    items: ShiftReportItem[];
    summary: ShiftSummaryStats;
}

/**
 * Табличное представление отчёта по смене с возможностью раскрытия замеров по машине.
 */
const ShiftReportTable: React.FC<ShiftReportTableProps> = ({ items, summary }) => {
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
            {/* Блок общей статистики */}
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

            {/* Таблица с возможностью раскрытия */}
            <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-700">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                №
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Машина
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Замеров
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Средняя влажность
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Мин / Макс
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Авто / Ручные
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Последний замер
                            </th>
                            <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Действия
                            </th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {items.map((item, index) => {
                            const isExpanded = expandedIds.has(item.vehicleId);
                            return (
                                <React.Fragment key={item.vehicleId}>
                                    {/* Основная строка */}
                                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800 transition duration-150">
                                        <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                                            {index + 1}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                            {item.number} ({item.vehiclePlate})
                                        </td>
                                        <td className="px-4 py-3 text-sm text-center font-medium text-gray-700 dark:text-gray-300">
                                            {item.measurementsCount}
                                        </td>
                                        <td className="px-4 py-3 text-sm font-semibold text-gray-900 dark:text-white">
                                            {item.averageHumidity !== null ? item.averageHumidity.toFixed(1) + '%' : '—'}
                                        </td>
                                        <td className="px-4 py-3 text-sm">
                                            {item.minHumidity !== null && item.maxHumidity !== null ? (
                                                <>
                                                    <span className="font-medium text-blue-600 dark:text-blue-400">
                                                        {item.minHumidity.toFixed(1)}%
                                                    </span>
                                                    <span className="text-gray-400 dark:text-gray-500 mx-1">/</span>
                                                    <span className="font-medium text-red-600 dark:text-red-400">
                                                        {item.maxHumidity.toFixed(1)}%
                                                    </span>
                                                </>
                                            ) : (
                                                <span className="text-gray-400 dark:text-gray-500">—</span>
                                            )}
                                        </td>
                                        <td className="px-4 py-3 text-sm">
                                            <span className="font-medium text-blue-600 dark:text-blue-400">
                                                {item.autoCount}
                                            </span>
                                            <span className="text-gray-400 dark:text-gray-500 mx-1">/</span>
                                            <span className="font-medium text-orange-600 dark:text-orange-400">
                                                {item.manualCount}
                                            </span>
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                            {item.lastMeasurementTimestamp
                                                ? format(
                                                    new Date(item.lastMeasurementTimestamp),
                                                    'dd MMM yyyy HH:mm',
                                                    { locale: ru }
                                                )
                                                : '—'}
                                        </td>
                                        <td className="px-4 py-3 text-center">
                                            <button
                                                onClick={() => toggleExpand(item.vehicleId)}
                                                className="inline-flex items-center gap-1 text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition"
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
                                        </td>
                                    </tr>

                                    {/* Строка с раскрытыми замерами */}
                                    {isExpanded && (
                                        <tr>
                                            <td colSpan={8} className="px-4 py-2 bg-gray-50 dark:bg-gray-800/50">
                                                <VehicleMeasurementsExpand vehicleId={item.vehicleId} compact={false} />
                                            </td>
                                        </tr>
                                    )}
                                </React.Fragment>
                            );
                        })}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

export default ShiftReportTable;