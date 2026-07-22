import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import { type MeasurementDto, MeasurementSource, SignType, type CreateMeasurementRequest, type UpdateMeasurementRequest } from '../types';
import { type MeasurementFormData, CreateMeasurementFormData } from '../schemas/measurementSchema';
import { measurementService } from '../services/api';

interface MeasurementFormModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
    vehicleId: string;
    measurement?: MeasurementDto | null;
}

export default function MeasurementFormModal({
    isOpen,
    onClose,
    onSuccess,
    vehicleId,
    measurement,
}: MeasurementFormModalProps) {
    const isEdit = !!measurement;

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors, isSubmitting },
    } = useForm<MeasurementFormData>({
        resolver: zodResolver(CreateMeasurementFormData),
        defaultValues: {
            vehicleId: vehicleId,
            humidityValue: 0,
            temperatureC: 20,
            measurementType: '',
            material: '',
            source: 'Auto' as const,
            timestamp: '',
            sign: 'None' as const,
        },
    });

    useEffect(() => {
        if (measurement) {
            reset({
                vehicleId: measurement.vehicleId,
                humidityValue: measurement.humidityValue,
                temperatureC: measurement.temperatureC,
                measurementType: measurement.measurementType,
                material: measurement.material,
                source: measurement.source === MeasurementSource.Auto ? 'Auto' : 'Manual',
                timestamp: measurement.timestamp.slice(0, 16),
                sign: measurement.sign === SignType.None ? 'None' : measurement.sign === SignType.Less ? 'Less' : 'Greater',
            });
        } else {
            reset({
                vehicleId: vehicleId,
                humidityValue: 0,
                temperatureC: 20,
                measurementType: '',
                material: '',
                source: 'Auto',
                timestamp: new Date().toISOString().slice(0, 16),
                sign: 'None',
            });
        }
    }, [measurement, vehicleId, reset]);

    const onSubmit = async (data: MeasurementFormData) => {
        try {
            const payload: CreateMeasurementRequest = {
                vehicleId: data.vehicleId,
                humidityValue: data.humidityValue,
                temperatureC: data.temperatureC,
                measurementType: data.measurementType,
                material: data.material,
                source: data.source === 'Auto' ? MeasurementSource.Auto : MeasurementSource.Manual,
                timestamp: new Date(data.timestamp).toISOString(),
                sign: data.sign === 'None' ? SignType.None : data.sign === 'Less' ? SignType.Less : SignType.Greater,
            };

            if (isEdit && measurement) {
                const updatePayload: UpdateMeasurementRequest = {
                    humidityValue: payload.humidityValue,
                    temperatureC: payload.temperatureC,
                    measurementType: payload.measurementType,
                    material: payload.material,
                    source: payload.source,
                    sign: payload.sign,
                    timestamp: payload.timestamp,
                };
                await measurementService.update(measurement.id, updatePayload);
                toast.success('Замер обновлён');
            } else {
                await measurementService.create(payload);
                toast.success('Замер создан');
            }
            onSuccess();
            onClose();
        } catch (error: any) {
            toast.error(error.response?.data?.message || 'Ошибка сохранения замера');
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl max-w-md w-full p-6 relative animate-slide-up">
                <button
                    onClick={onClose}
                    className="absolute top-3 right-3 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                >
                    <X className="w-5 h-5" />
                </button>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                    {isEdit ? 'Редактировать замер' : 'Новый замер'}
                </h3>
                <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Влажность (%) *</label>
                        <input
                            type="number"
                            step="0.1"
                            {...register('humidityValue', { valueAsNumber: true })}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.humidityValue && <p className="text-red-500 text-xs mt-1">{errors.humidityValue.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Температура (°C) *</label>
                        <input
                            type="number"
                            step="0.1"
                            {...register('temperatureC', { valueAsNumber: true })}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.temperatureC && <p className="text-red-500 text-xs mt-1">{errors.temperatureC.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Тип измерения *</label>
                        <input
                            {...register('measurementType')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.measurementType && <p className="text-red-500 text-xs mt-1">{errors.measurementType.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Материал *</label>
                        <input
                            {...register('material')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.material && <p className="text-red-500 text-xs mt-1">{errors.material.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Источник *</label>
                        <select
                            {...register('source')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        >
                            <option value="Auto">Авто</option>
                            <option value="Manual">Ручной</option>
                        </select>
                        {errors.source && <p className="text-red-500 text-xs mt-1">{errors.source.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Знак *</label>
                        <select
                            {...register('sign')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        >
                            <option value="None">Нет</option>
                            <option value="Less">&lt; (меньше)</option>
                            <option value="Greater">&gt; (больше)</option>
                        </select>
                        {errors.sign && <p className="text-red-500 text-xs mt-1">{errors.sign.message}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Дата и время *</label>
                        <input
                            type="datetime-local"
                            {...register('timestamp')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.timestamp && <p className="text-red-500 text-xs mt-1">{errors.timestamp.message}</p>}
                    </div>
                    <div className="flex justify-end gap-3 mt-6">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition"
                        >
                            Отмена
                        </button>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition disabled:opacity-50"
                        >
                            {isSubmitting ? 'Сохранение...' : isEdit ? 'Обновить' : 'Создать'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}