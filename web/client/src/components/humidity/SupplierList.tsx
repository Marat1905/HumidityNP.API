import React from 'react';
import type { SupplierDto } from '../../types/humidity';
import { ChevronRight, Truck, Activity, Droplet } from 'lucide-react';
import SupplierDetails from './SupplierDetails';

interface SupplierListProps {
    suppliers: SupplierDto[];
    expandedInn: string | null;
    onToggle: (inn: string) => void;
    fromDate: Date | null;   // <-- добавить
    toDate: Date | null;     // <-- добавить
}

const SupplierList: React.FC<SupplierListProps> = ({
    suppliers,
    expandedInn,
    onToggle,
    fromDate,
    toDate,
}) => {
    if (suppliers.length === 0) {
        return <div className="text-center py-8 text-gray-500 dark:text-gray-400">Нет поставщиков за выбранный период</div>;
    }

    return (
        <div className="space-y-2">
            {suppliers.map((supplier) => (
                <div
                    key={supplier.inn}
                    className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden transition-all"
                >
                    <button
                        onClick={() => onToggle(supplier.inn)}
                        className="w-full flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition text-left"
                    >
                        <div className="flex-1 grid grid-cols-1 sm:grid-cols-4 gap-2 items-center">
                            <div>
                                <div className="text-sm font-semibold text-gray-900 dark:text-white">{supplier.counterparty}</div>
                                <div className="text-xs text-gray-500 dark:text-gray-400">ИНН: {supplier.inn}</div>
                            </div>
                            <div className="flex items-center gap-1 text-sm text-gray-700 dark:text-gray-300">
                                <Truck className="w-4 h-4 text-blue-500" />
                                <span>{supplier.vehiclesCount} машин</span>
                            </div>
                            <div className="flex items-center gap-1 text-sm text-gray-700 dark:text-gray-300">
                                <Activity className="w-4 h-4 text-indigo-500" />
                                <span>{supplier.totalMeasurements} замеров</span>
                            </div>
                            <div className="flex items-center gap-1 text-sm font-medium text-gray-900 dark:text-white">
                                <Droplet className="w-4 h-4 text-emerald-500" />
                                <span>Ср. влажность: {supplier.averageHumidity !== null ? supplier.averageHumidity.toFixed(1) + '%' : '—'}</span>
                            </div>
                        </div>
                        <ChevronRight
                            className={`w-5 h-5 text-gray-400 transition-transform duration-200 ${expandedInn === supplier.inn ? 'rotate-90' : ''}`}
                        />
                    </button>

                    {expandedInn === supplier.inn && (
                        <div className="border-t border-gray-200 dark:border-gray-700 p-4 bg-gray-50 dark:bg-gray-900/50">
                            {/* Передаём даты в SupplierDetails */}
                            <SupplierDetails
                                supplierInn={supplier.inn}
                                fromDate={fromDate}
                                toDate={toDate}
                            />
                        </div>
                    )}
                </div>
            ))}
        </div>
    );
};

export default SupplierList;