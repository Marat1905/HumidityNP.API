// src/components/ShiftReportTable.tsx
import React from 'react';
import type { ShiftReportItem, ShiftSummaryStats } from '../hooks/useShiftReport';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';

interface ShiftReportTableProps {
    items: ShiftReportItem[];
    summary: ShiftSummaryStats;
}

/**
 * Компонент отображения сводной таблицы отчёта по смене с общей статистикой.
 */
const ShiftReportTable: React.FC<ShiftReportTableProps> = ({ items, summary }) => {
    if (items.length === 0) {
        return (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                За выбранную смену нет замеров.
            </div>
        );
    }

    return (
        <div>
            {/* Блок общей статистики */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Машин</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">{summary.vehicleCount}</div>
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Всего замеров</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">{summary.totalMeasurements}</div>
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Средняя влажность</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">
                        {summary.overallAverageHumidity !== null ? summary.overallAverageHumidity.toFixed(1) + '%' : '—'}
                    </div>
                </div>
                <div>
                    <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Авто / Ручные</div>
                    <div className="text-2xl font-bold text-gray-900 dark:text-white">
                        {summary.totalAutoCount} / {summary.totalManualCount}
                    </div>
                </div>
            </div>

            {/* Таблица по машинам */}
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                №
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Машина (ID)
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Кол-во замеров
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
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {items.map((item, index) => (
                            <tr key={item.vehicleId} className="hover:bg-gray-50 dark:hover:bg-gray-800 transition">
                                <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                                    {index + 1}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-900 dark:text-white font-mono">
                                    {item.vehicleId.slice(0, 8)}...
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                    {item.measurementsCount}
                                </td>
                                <td className="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                                    {item.averageHumidity !== null ? item.averageHumidity.toFixed(1) + '%' : '—'}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {item.minHumidity !== null && item.maxHumidity !== null
                                        ? `${item.minHumidity.toFixed(1)}% / ${item.maxHumidity.toFixed(1)}%`
                                        : '—'}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {item.autoCount} / {item.manualCount}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {item.lastMeasurementTimestamp
                                        ? format(new Date(item.lastMeasurementTimestamp), 'dd MMM yyyy HH:mm', { locale: ru })
                                        : '—'}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

export default ShiftReportTable;