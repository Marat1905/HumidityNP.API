import { z } from 'zod';

/**
 * Схема валидации формы фиксации разгрузки машины.
 * Повторяет правила UnloadVehicleRequestValidator с бэкенда.
 */
export const UnloadVehicleFormData = z.object({
    baleCount: z.coerce.number()
        .min(0, 'Количество тюков не может быть отрицательным'),
    damagedBaleCount: z.coerce.number()
        .min(0, 'Количество порванных тюков не может быть отрицательным'),
    weightKg: z.coerce.number()
        .min(0, 'Вес не может быть отрицательным'),
    stackNumber: z.string()
        .min(1, 'Номер штабеля обязателен')
        .max(50, 'Номер штабеля не должен превышать 50 символов'),
}).refine((data) => data.damagedBaleCount <= data.baleCount, {
    message: 'Количество порванных тюков не может превышать общее количество',
    path: ['damagedBaleCount'],
}).refine((data) => {
    // Если порванных тюков больше 0, вес должен быть > 0
    if (data.damagedBaleCount > 0) return data.weightKg > 0;
    return true;
}, {
    message: 'При наличии порванных тюков вес должен быть больше 0',
    path: ['weightKg'],
});

export type UnloadVehicleSchemaType = z.infer<typeof UnloadVehicleFormData>;