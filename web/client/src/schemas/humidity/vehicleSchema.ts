import { z } from 'zod';

export const CreateVehicleFormData = z.object({
    number: z.string().min(1, 'Номер пропуска обязателен'),
    date: z.string().min(1, 'Дата создания пропуска обязательна'),
    entryDate: z.string().min(1, 'Дата въезда обязательна'),
    exitDate: z.string().optional(),
    counterparty: z.string().min(1, 'Поставщик обязателен'),
    inn: z.string().optional().nullable(), // ИНН поставщика, необязательно
    vehicleBrand: z.string().min(1, 'Марка автомобиля обязательна'),
    vehiclePlate: z.string().min(1, 'Государственный номер обязателен'),
    trailer: z.string().optional(),
    driver: z.string().min(1, 'ФИО водителя обязательно'),
});

export type VehicleFormData = z.infer<typeof CreateVehicleFormData>;