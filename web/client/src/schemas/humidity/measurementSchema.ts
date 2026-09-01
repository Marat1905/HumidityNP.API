import { z } from 'zod';

export const CreateMeasurementFormData = z.object({
    vehicleId: z.string().uuid('Выберите машину'),
    humidityValue: z.coerce.number().min(0).max(100, 'Влажность должна быть от 0 до 100'),
    temperatureC: z.coerce.number().min(-50).max(100, 'Температура должна быть от -50 до 100'),
    // Поля сделаны необязательными (могут быть пустой строкой или null)
    measurementType: z.string().optional(),
    material: z.string().optional(),
    source: z.enum(['Auto', 'Manual']),
    timestamp: z.string().min(1, 'Дата и время обязательны'),
    sign: z.enum(['Less', 'Greater', 'None']),
});

export type MeasurementFormData = z.infer<typeof CreateMeasurementFormData>;