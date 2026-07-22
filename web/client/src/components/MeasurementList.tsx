import { useState } from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { Pencil, Trash2 } from 'lucide-react';
import { type MeasurementDto, SignType, MeasurementSource } from '../types';
import Pagination from './Pagination';
import DeleteConfirmationModal from './DeleteConfirmationModal';
import MeasurementFormModal from './MeasurementFormModal';
import { measurementService } from '../services/api';
import toast from 'react-hot-toast';

interface MeasurementListProps {
    vehicleId: string;
    measurements: MeasurementDto[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (size: number) => void;
    onRefresh: () => void;
    loading: boolean;
}

export default function MeasurementList({
    vehicleId,
    measurements,
    totalCount,
    pageNumber,
    pageSize,
    totalPages,
    onPageChange,
    onPageSizeChange,
    onRefresh,
    loading,
}: MeasurementListProps) {
    const [editMeasurement, setEditMeasurement] = useState<MeasurementDto | null>(null);
    const [deleteId, setDeleteId] = useState<string | null>(null);
    const [showCreateModal, setShowCreateModal] = useState(false);

    const handleDelete = async () => {
        if (!deleteId) return;
        try {
            await measurementService.delete(deleteId);
            toast.success('Замер удалён');
            onRefresh();
        } catch (error: any) {
            toast.error(error.response?.data?.message || 'Ошибка удаления');
        } finally {
            setDeleteId(null);
        }
    };

    const getSignSymbol = (sign: SignType) => {
        switch (sign) {
            case SignType.Less: return '<';
            case SignType.Greater: return '>';
            default: return '';
        }
    };

    const getSourceLabel = (source: MeasurementSource) => {
        return source === MeasurementSource.Auto ? 'Авто' : 'Ручной';
    };

    if (loading) {
        return <div className="text-center py-4 text-gray-500 dark:text-gray-400">Загрузка замеров...</div>;
    }

    return (
        <div className="mt-6">
            <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Замеры влажности</h3>
                <button
                    onClick={() => setShowCreateModal(true)}
                    className="px-3 py-1.5 text-sm font-medium text-white bg-green-600 rounded-lg hover:bg-green-700 transition"
                >
                    + Добавить замер
                </button>
            </div>

            {measurements.length === 0 ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400">Нет замеров для этой машины</div>
            ) : (
                <>
                    <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                            <thead className="bg-gray-50 dark:bg-gray-800">
                                <tr>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                        Время
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
                                {measurements.map((m) => (
                                    <tr key={m.id} className="hover:bg-gray-50 dark:hover:bg-gray-800 transition">
                                        <td className="px-4 py-3 text-sm text-gray-900 dark:text-white whitespace-nowrap">
                                            {format(new Date(m.timestamp), 'dd MMM yyyy HH:mm', { locale: ru })}
                                        </td>
                                        <td className="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                                            {m.displayValue}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                            {m.temperatureC.toFixed(1)} °C
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                            {m.material}
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
                        onPageChange={onPageChange}
                        pageSize={pageSize}
                        onPageSizeChange={onPageSizeChange}
                        totalCount={totalCount}
                    />
                </>
            )}

            {/* Модалка создания/редактирования замера */}
            <MeasurementFormModal
                isOpen={showCreateModal || !!editMeasurement}
                onClose={() => {
                    setShowCreateModal(false);
                    setEditMeasurement(null);
                }}
                onSuccess={() => {
                    onRefresh();
                    setShowCreateModal(false);
                    setEditMeasurement(null);
                }}
                vehicleId={vehicleId}
                measurement={editMeasurement}
            />

            {/* Модалка удаления */}
            <DeleteConfirmationModal
                isOpen={!!deleteId}
                onClose={() => setDeleteId(null)}
                onConfirm={handleDelete}
                message="Вы уверены, что хотите удалить этот замер?"
            />
        </div>
    );
}