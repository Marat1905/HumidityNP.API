import React from 'react';
import type { SupplierDto } from '../../types/humidity';

interface SuppliersTableProps {
    suppliers: SupplierDto[];
    rankType: 'good' | 'bad';
}

const SuppliersTable: React.FC<SuppliersTableProps> = ({ suppliers, rankType }) => {
    // ИСПРАВЛЕНИЕ: безопасная обработка пропса suppliers на случай, если он не является массивом
    const safeSuppliers = Array.isArray(suppliers) ? suppliers : [];

    const getRowColor = (index: number) => {
        if (rankType === 'good') {
            if (index === 0) return 'bg-green-50 dark:bg-green-900/20 border-green-300 dark:border-green-700';
            if (index === 1) return 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700';
            if (index === 2) return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-300 dark:border-yellow-700';
        } else {
            if (index === 0) return 'bg-red-50 dark:bg-red-900/20 border-red-300 dark:border-red-700';
            if (index === 1) return 'bg-orange-50 dark:bg-orange-900/20 border-orange-300 dark:border-orange-700';
            if (index === 2) return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-300 dark:border-yellow-700';
        }
        return '';
    };

    // ИСПРАВЛЕНИЕ: если suppliers пустой или не массив, показываем заглушку вместо падения
    if (safeSuppliers.length === 0) {
        return (
            <div className="text-center text-gray-500 dark:text-gray-400 py-12 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700">
                <p>Нет данных для отображения</p>
            </div>
        );
    }

    return (
        <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                    <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            №
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            Поставщик
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            Средняя влажность
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            Замеров
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            Машин
                        </th>
                    </tr>
                </thead>
                <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                    {safeSuppliers.map((supplier, index) => (
                        <tr
                            key={supplier.inn}
                            className={`hover:bg-gray-50 dark:hover:bg-gray-800 transition ${getRowColor(index)}`}
                        >
                            <td className="px-4 py-3 text-sm font-bold text-gray-700 dark:text-gray-300">
                                {index + 1}
                            </td>
                            <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                <div>
                                    <div className="font-medium">{supplier.counterparty}</div>
                                    <div className="text-xs text-gray-500 dark:text-gray-400">ИНН: {supplier.inn}</div>
                                </div>
                            </td>
                            <td className="px-4 py-3 text-sm font-semibold">
                                <span className={rankType === 'good' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
                                    {supplier.averageHumidity !== null ? supplier.averageHumidity.toFixed(1) + '%' : '—'}
                                </span>
                            </td>
                            <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                {supplier.totalMeasurements}
                            </td>
                            <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                {supplier.vehiclesCount}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
};

export default SuppliersTable;