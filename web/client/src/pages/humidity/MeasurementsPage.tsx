import { useState, useEffect } from 'react';
import { format, subDays } from 'date-fns';
import { ru } from 'date-fns/locale';
import { Pencil, Trash2, RotateCcw } from 'lucide-react';
import { useMeasurementsByDateRange } from '../../hooks/humidity/useMeasurementsByDateRange';
import Pagination from '../../components/common/Pagination';
import { SkeletonTable } from '../../components/common/Skeleton';
import DeleteConfirmationModal from '../../components/humidity/DeleteConfirmationModal';
import MeasurementFormModal from '../../components/humidity/MeasurementFormModal';
import RangeDatePicker from '../../components/common/RangeDatePicker';
import { measurementService } from '../../services/humidity/api';
import toast from 'react-hot-toast';
import type { MeasurementDto, SignType, MeasurementSource } from '../../types/humidity';

export default function MeasurementsPage() {
    // Количество дней по умолчанию для отображения (последние 2 недели)
    const DEFAULT_DAYS = 14;

    // Состояния для выбора диапазона дат (храним объекты Date)
    const [startDate, setStartDate] = useState<Date | null>(null);
    const [endDate, setEndDate] = useState<Date | null>(null);

    // При монтировании устанавливаем диапазон по умолчанию (последние 14 дней)
    useEffect(() => {
        const now = new Date();
        const from = subDays(now, DEFAULT_DAYS);
        setStartDate(from);
        setEndDate(now);
    }, []);

    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        setStartDate(start);
        setEndDate(end);
        setPageNumber(1); // сброс пагинации
    };

    // Сброс фильтра – возвращаем к диапазону по умолчанию (последние 14 дней)
    const resetFilter = () => {
        const now = new Date();
        const from = subDays(now, DEFAULT_DAYS);
        setStartDate(from);
        setEndDate(now);
        setPageNumber(1);
    };

    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    const { data, loading, error, refetch } = useMeasurementsByDateRange(
        startDate,
        endDate,
        pageNumber,
        pageSize
    );

    const [editMeasurement, setEditMeasurement] = useState<MeasurementDto | null>(null);
    const [deleteId, setDeleteId] = useState<string | null>(null);

    const handleDelete = async () => {
        if (!deleteId) return;
        try {
            await measurementService.delete(deleteId);
            toast.success('Замер удалён');
            refetch();
        } catch (err: any) {
            toast.error(err.response?.data?.message || 'Ошибка удаления');
        } finally {
            setDeleteId(null);
        }
    };

    const getSignSymbol = (sign: SignType) => {
        switch (sign) {
            case 'Less': return '<';
            case 'Greater': return '>';
            default: return '';
        }
    };

    const getSourceLabel = (source: MeasurementSource) => {
        return source === 'Auto' ? 'Авто' : 'Ручной';
    };

    if (loading) return <SkeletonTable rows={5} columns={7} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;
    if (!data && !loading) {
        return (
            <div className="text-center py-10 text-gray-500 dark:text-gray-400">
                <p>Выберите диапазон дат для отображения замеров.</p>
                <div className="mt-4 max-w-xs mx-auto">
                    <RangeDatePicker
                        startDate={startDate}
                        endDate={endDate}
                        onChange={handleDateRangeChange}
                        size="md"
                    />
                </div>
            </div>
        );
    }

    // ИСПРАВЛЕНИЕ: безопасное извлечение свойств с резервными значениями
    const items = data?.items ?? [];
    const totalCount = data?.totalCount ?? 0;
    const totalPages = data?.totalPages ?? 0;

    return (
        <div>
            {/* Фильтр по дате – пикер и кнопка справа */}
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <span className="text-sm text-gray-600 dark:text-gray-300">
                    Показаны замеры с {format(startDate!, 'dd.MM.yyyy')} по {format(endDate!, 'dd.MM.yyyy')}
                </span>
                <div className="flex items-center gap-2">
                    <div className="w-64">
                        <RangeDatePicker
                            startDate={startDate}
                            endDate={endDate}
                            onChange={handleDateRangeChange}
                            size="sm"
                        />
                    </div>
                    <button
                        onClick={resetFilter}
                        className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                        title="Сбросить фильтр к последним 14 дням"
                    >
                        <RotateCcw className="w-4 h-4" />
                        Сбросить
                    </button>
                </div>
            </div>

            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Время
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Машина
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Влажность
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Температура
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Материал
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Источник
                            </th>
                            <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Действия
                            </th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {items.map((m) => (
                            <tr key={m.id} className="hover:bg-gray-50 dark:hover:bg-gray-800 transition">
                                <td className="px-4 py-3 text-sm text-gray-900 dark:text-white whitespace-nowrap">
                                    {format(new Date(m.timestamp), 'dd MMM yyyy HH:mm', { locale: ru })}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {m.vehicleNumber} ({m.vehiclePlate})
                                </td>
                                <td className="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                                    {m.displayValue}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {m.temperatureC.toFixed(1)} °C
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {m.material ?? '—'}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {getSourceLabel(m.source)}
                                </td>
                                <td className="px-4 py-3 text-right">
                                    <div className="flex justify-end gap-2">
                                        <button
                                            onClick={() => setEditMeasurement(m)}
                                            className="p-1 text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 transition"
                                        >
                                            <Pencil className="w-4 h-4" />
                                        </button>
                                        <button
                                            onClick={() => setDeleteId(m.id)}
                                            className="p-1 text-red-600 hover:text-red-800 dark:text-red-400 dark:hover:text-red-300 transition"
                                        >
                                            <Trash2 className="w-4 h-4" />
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                onPageChange={setPageNumber}
                pageSize={pageSize}
                onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                totalCount={totalCount}
            />

            {/* Модалка редактирования замера */}
            {editMeasurement && (
                <MeasurementFormModal
                    isOpen={true}
                    onClose={() => setEditMeasurement(null)}
                    onSuccess={() => {
                        refetch();
                        setEditMeasurement(null);
                    }}
                    vehicleId={editMeasurement.vehicleId}
                    measurement={editMeasurement}
                />
            )}

            <DeleteConfirmationModal
                isOpen={!!deleteId}
                onClose={() => setDeleteId(null)}
                onConfirm={handleDelete}
                message="Вы уверены, что хотите удалить этот замер?"
            />
        </div>
    );
}