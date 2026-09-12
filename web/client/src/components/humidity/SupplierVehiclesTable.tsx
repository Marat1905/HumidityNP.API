import React, { useState } from 'react';
import type { SupplierVehicleSummaryDto } from '../../types/humidity';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { Truck, Activity, Droplet, Zap, PenTool } from 'lucide-react';
import { Pagination } from '../common';

interface SupplierVehiclesTableProps {
    vehicles: SupplierVehicleSummaryDto[];
}

const SupplierVehiclesTable: React.FC<SupplierVehiclesTableProps> = ({ vehicles }) => {
    // Состояние для пагинации таблицы машин
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    if (vehicles.length === 0) {
        return <div className="text-center py-4 text-gray-500 dark:text-gray-400">Нет машин</div>;
    }

    // Вычисляем данные для текущей страницы
    const totalPages = Math.ceil(vehicles.length / pageSize);
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedVehicles = vehicles.slice(startIndex, endIndex);

    return (
        <div className="space-y-4">
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                        <thead className="bg-gray-50 dark:bg-gray-700">
                            <tr>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Машина
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Дата въезда
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Замеров
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Средняя влажность
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Мин / Макс
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Авто / Ручные
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Последний замер
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                            {paginatedVehicles.map((v) => (
                                <tr key={v.vehicleId} className="hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                        <div className="flex items-center gap-2">
                                            <Truck className="w-4 h-4 text-blue-500" />
                                            <span>{v.number}</span>
                                            <span className="text-gray-500 dark:text-gray-400">({v.vehiclePlate})</span>
                                        </div>
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                        {format(new Date(v.entryDate), 'dd MMM yyyy HH:mm', { locale: ru })}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-center font-medium text-gray-900 dark:text-white">
                                        {v.measurementsCount}
                                    </td>
                                    <td className="px-4 py-3 text-sm font-semibold text-gray-900 dark:text-white">
                                        {v.averageHumidity !== null ? v.averageHumidity.toFixed(1) + '%' : '—'}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                        {v.minHumidity !== null && v.maxHumidity !== null ? (
                                            <>
                                                <span className="text-blue-600 dark:text-blue-400">{v.minHumidity.toFixed(1)}%</span>
                                                <span className="text-gray-400 mx-1">/</span>
                                                <span className="text-red-600 dark:text-red-400">{v.maxHumidity.toFixed(1)}%</span>
                                            </>
                                        ) : (
                                            '—'
                                        )}
                                    </td>
                                    <td className="px-4 py-3 text-sm">
                                        <span className="font-medium text-blue-600 dark:text-blue-400">{v.autoCount}</span>
                                        <span className="text-gray-400 mx-1">/</span>
                                        <span className="font-medium text-orange-600 dark:text-orange-400">{v.manualCount}</span>
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                        {v.lastMeasurementTimestamp
                                            ? format(new Date(v.lastMeasurementTimestamp), 'dd MMM HH:mm', { locale: ru })
                                            : '—'}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Компонент пагинации для таблицы машин */}
            {totalPages > 1 && (
                <Pagination
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPageChange={setCurrentPage}
                    pageSize={pageSize}
                    onPageSizeChange={(size) => { setPageSize(size); setCurrentPage(1); }}
                    totalCount={vehicles.length}
                />
            )}
        </div>
    );
};

export default SupplierVehiclesTable;