import React from 'react';
import { useSupplierDetails } from '../../hooks/humidity/useSupplierDetails';
import { SkeletonReport } from '../common/Skeleton';
import SupplierChart from './SupplierChart';
import SupplierVehiclesTable from './SupplierVehiclesTable';
import MeasurementStatistics from '../humidity/MeasurementStatistics';

interface SupplierDetailsProps {
    supplierInn: string;
    fromDate?: Date;
    toDate?: Date;
}

const SupplierDetails: React.FC<SupplierDetailsProps> = ({ supplierInn, fromDate, toDate }) => {
    // Если даты не переданы, используем глобальный контекст (будет передан из родителя)
    // Для простоты будем использовать пропсы, а на странице передадим глобальный период.
    const { data, loading, error } = useSupplierDetails(supplierInn, fromDate || null, toDate || null);

    if (loading) return <SkeletonReport />;
    if (error) return <div className="text-red-500 text-center py-4">{error.message}</div>;
    if (!data || data.vehicles.length === 0) {
        return <div className="text-center py-4 text-gray-500 dark:text-gray-400">Нет данных по этому поставщику за выбранный период</div>;
    }

    return (
        <div className="space-y-6">
            {/* Общая статистика */}
            <MeasurementStatistics stats={data.overallStatistics} />

            {/* График изменения влажности по машинам */}
            <SupplierChart vehicles={data.vehicles} />

            {/* Таблица с деталями по машинам */}
            <SupplierVehiclesTable vehicles={data.vehicles} />
        </div>
    );
};

export default SupplierDetails;