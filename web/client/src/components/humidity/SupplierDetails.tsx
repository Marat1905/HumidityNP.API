import React, { useState } from 'react';
import { useSupplierDetails, useSupplierChartData } from '../../hooks/humidity';
import { SkeletonReport } from '../common';
import { SupplierChart, SupplierVehiclesTable, MeasurementStatistics } from '../humidity';

interface SupplierDetailsProps {
    supplierInn: string;
    fromDate?: Date;
    toDate?: Date;
}

/**
 * Компонент детальной информации по поставщику.
 * Содержит:
 *  - общую статистику по всем машинам за период (приходит с сервера);
 *  - график диапазона влажности по всем машинам (без пагинации, сортировка с сервера);
 *  - таблицу машин с постраничной навигацией (пагинация и сортировка с сервера).
 */
const SupplierDetails: React.FC<SupplierDetailsProps> = ({ supplierInn, fromDate, toDate }) => {
    // Состояние пагинации для таблицы машин.
    // Пагинация выполняется на сервере, поэтому при изменении этих значений выполняется новый запрос.
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    // Сортировка по дате въезда. По умолчанию — новые сверху.
    const sortOrder: 'asc' | 'desc' = 'desc';

    // Загружаем постраничные детали поставщика (включая общую статистику)
    const { data, loading, error } = useSupplierDetails(
        supplierInn,
        fromDate || null,
        toDate || null,
        pageNumber,
        pageSize,
        sortOrder
    );

    // Отдельно загружаем полный список машин для графика (без пагинации, сортировка с сервера)
    const {
        data: chartData,
        loading: chartLoading,
        error: chartError,
    } = useSupplierChartData(
        supplierInn,
        fromDate || null,
        toDate || null,
        sortOrder
    );

    if (loading) return <SkeletonReport />;
    if (error) return <div className="text-red-500 text-center py-4">{error.message}</div>;
    if (!data || data.vehicles.items.length === 0) {
        return (
            <div className="text-center py-4 text-gray-500 dark:text-gray-400">
                Нет данных по этому поставщику за выбранный период
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Общая статистика (по всем машинам за период) */}
            <MeasurementStatistics stats={data.overallStatistics} />

            {/* График: полный список машин, отсортированный на сервере, без пагинации */}
            {chartLoading ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                    Загрузка графика...
                </div>
            ) : chartError ? (
                <div className="text-red-500 text-center py-4">{chartError.message}</div>
            ) : (
                <SupplierChart vehicles={chartData} />
            )}

            {/* Таблица машин с серверной пагинацией */}
            <SupplierVehiclesTable
                vehicles={data.vehicles}
                onPageChange={setPageNumber}
                onPageSizeChange={(size) => {
                    setPageSize(size);
                    setPageNumber(1);
                }}
            />
        </div>
    );
};

export default SupplierDetails;