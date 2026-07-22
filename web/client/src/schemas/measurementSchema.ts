import { z } from 'zod';

export const measurementSchema = z.object({
    vehicleId: z.string().uuid('Выберите машину'),
    humidityValue: z.coerce.number().min(0, 'Влажность не может быть отрицательной').max(100, 'Влажность не может быть больше 100'),
    temperatureC: z.coerce.number(),
    measurementType: z.string().min(1, 'Тип измерения обязателен'),
    material: z.string().min(1, 'Материал обязателен'),
    source: z.enum(['Auto', 'Manual']),
    timestamp: z.string().min(1, 'Дата и время обязательны'),
    sign: z.enum(['Less', 'Greater', 'None']),
});

export type MeasurementFormData = z.infer<typeof measurementSchema>;