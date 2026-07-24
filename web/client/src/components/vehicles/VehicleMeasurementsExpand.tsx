import { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { useMeasurements } from '../../hooks/useMeasurements';
import Pagination from '../shared/Pagination';
import { SkeletonMeasurementsList } from '../shared/Skeleton';
import type { MeasurementDto, SignType, MeasurementSource } from '../../types';

interface VehicleMeasurementsExpandProps {
    vehicleId: string;
    /** Если true, показывает замеры в свёрнутом виде (например, только первые 5) – опционально */
    compact?: boolean;
}

/**
 * Компонент для отображения замеров конкретной машины с пагинацией.
 * Используется внутри раскрывающихся блоков таблицы/карточек.
 * Столбец «Машина» опущен, так как контекст уже известен.
 */
export default function VehicleMeasurementsExpand({
    vehicleId,
    compact = false,
}: VehicleMeasurementsExpandProps) {
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(compact ? 5 : 10);

    const { data, loading, error } = useMeasurements(vehicleId, pageNumber, pageSize);

    if (loading) return <SkeletonMeasurementsList compact={compact} />;
    if (error) return <div className="text-red-500 text-sm">{error.message}</div>;
    if (!data || data.items.length === 0) {
        return <div className="text-gray-500 dark:text-gray-400 text-sm py-2">Нет замеров</div>;
    }

    const getSourceLabel = (source: MeasurementSource) => {
        return source === 'Auto' ? 'Авто' : 'Ручной';
    };

    return (
        <div className="py-2">
            <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Время
                            </th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Влажность
                            </th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Темп.
                            </th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Материал
                            </th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Источник
                            </th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {data.items.map((m) => (
                            <tr key={m.id} className="hover:bg-gray-50 dark:hover:bg-gray-800">
                                <td className="px-3 py-2 whitespace-nowrap text-gray-900 dark:text-white">
                                    {format(new Date(m.timestamp), 'dd MMM HH:mm', { locale: ru })}
                                </td>
                                <td className="px-3 py-2 font-medium text-gray-900 dark:text-white">
                                    {m.displayValue}
                                </td>
                                <td className="px-3 py-2 text-gray-700 dark:text-gray-300">
                                    {m.temperatureC.toFixed(1)} °C
                                </td>
                                <td className="px-3 py-2 text-gray-700 dark:text-gray-300">
                                    {m.material ?? '—'}
                                </td>
                                <td className="px-3 py-2 text-gray-700 dark:text-gray-300">
                                    {getSourceLabel(m.source)}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            <Pagination
                currentPage={data.pageNumber}
                totalPages={data.totalPages}
                onPageChange={setPageNumber}
                pageSize={data.pageSize}
                onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                totalCount={data.totalCount}
            />
        </div>
    );
}