import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import { type UnloadVehicleRequest } from '../../../types/humidity';
import { UnloadVehicleFormData, type UnloadVehicleSchemaType } from '../../../schemas/humidity';
import { vehicleService } from '../../../services/humidity/api';

interface UnloadVehicleModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
    vehicleId: string;
    initialData?: {
        baleCount?: number | null;
        damagedBaleCount?: number | null;
        weightKg?: number | null;
        stackNumber?: string | null;
    };
}

export default function UnloadVehicleModal({
    isOpen,
    onClose,
    onSuccess,
    vehicleId,
    initialData,
}: UnloadVehicleModalProps) {
    const [isSubmitting, setIsSubmitting] = useState(false);

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors },
    } = useForm<UnloadVehicleSchemaType>({
        resolver: zodResolver(UnloadVehicleFormData),
        defaultValues: {
            baleCount: initialData?.baleCount ?? 0,
            damagedBaleCount: initialData?.damagedBaleCount ?? 0,
            weightKg: initialData?.weightKg ?? 0,
            stackNumber: initialData?.stackNumber ?? '',
        },
    });

    // Сбрасываем форму при открытии, подставляя текущие значения (если есть)
    useEffect(() => {
        if (isOpen) {
            reset({
                baleCount: initialData?.baleCount ?? 0,
                damagedBaleCount: initialData?.damagedBaleCount ?? 0,
                weightKg: initialData?.weightKg ?? 0,
                stackNumber: initialData?.stackNumber ?? '',
            });
        }
    }, [isOpen, initialData, reset]);

    const onSubmit = async (data: UnloadVehicleSchemaType) => {
        setIsSubmitting(true);
        try {
            const payload: UnloadVehicleRequest = {
                baleCount: data.baleCount,
                damagedBaleCount: data.damagedBaleCount,
                weightKg: data.weightKg,
                stackNumber: data.stackNumber,
            };
            await vehicleService.unload(vehicleId, payload);
            toast.success('Разгрузка успешно зафиксирована');
            onSuccess();
            onClose();
        } catch (error: any) {
            toast.error(error.response?.data?.message || 'Ошибка сохранения данных разгрузки');
        } finally {
            setIsSubmitting(false);
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
                    Фиксация разгрузки
                </h3>
                <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                            Количество тюков *
                        </label>
                        <input
                            type="number"
                            {...register('baleCount', { valueAsNumber: true })}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.baleCount && <p className="text-red-500 text-xs mt-1">{errors.baleCount.message}</p>}
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                            Количество порванных тюков *
                        </label>
                        <input
                            type="number"
                            {...register('damagedBaleCount', { valueAsNumber: true })}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.damagedBaleCount && <p className="text-red-500 text-xs mt-1">{errors.damagedBaleCount.message}</p>}
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                            Вес (кг) *
                        </label>
                        <input
                            type="number"
                            step="0.1"
                            {...register('weightKg', { valueAsNumber: true })}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.weightKg && <p className="text-red-500 text-xs mt-1">{errors.weightKg.message}</p>}
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                            Номер штабеля *
                        </label>
                        <input
                            type="text"
                            {...register('stackNumber')}
                            className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        />
                        {errors.stackNumber && <p className="text-red-500 text-xs mt-1">{errors.stackNumber.message}</p>}
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
                            {isSubmitting ? 'Сохранение...' : 'Сохранить'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}