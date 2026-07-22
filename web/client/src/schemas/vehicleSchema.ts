import { z } from 'zod';

export const CreateVehicleFormData = z.object({
    number: z.string().min(1, 'Номер заявки обязателен'),
    date: z.string().min(1, 'Дата создания обязательна'),
    arrivalDate: z.string().min(1, 'Дата приезда обязательна'),
    entryDate: z.string().min(1, 'Дата въезда обязательна'),
    exitDate: z.string().optional(),
    counterparty: z.string().min(1, 'Контрагент обязателен'),
    workType: z.string().min(1, 'Вид работ обязателен'),
    vehicleBrand: z.string().min(1, 'Марка автомобиля обязательна'),
    vehiclePlate: z.string().min(1, 'Гос. номер обязателен'),
    trailer: z.string().optional(),
    driver: z.string().min(1, 'ФИО водителя обязательно'),
    loader: z.string().optional(),
    expeditor: z.string().optional(),
    department: z.string().min(1, 'Подразделение обязательно'),
});

export type VehicleFormData = z.infer<typeof CreateVehicleFormData>;