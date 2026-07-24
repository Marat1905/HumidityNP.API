export enum MeasurementSource {
    Auto = 'Auto',
    Manual = 'Manual'
}

export enum SignType {
    None = 'None',
    Less = 'Less',
    Greater = 'Greater'
}

export interface VehicleDto {
    id: string;
    number: string;                 // Номер пропуска
    date: string;                   // Дата создания пропуска
    entryDate: string;              // Дата въезда на площадку
    exitDate?: string;              // Дата выезда с площадки
    counterparty: string;           // Поставщик
    inn?: string | null;            // ИНН поставщика
    vehicleBrand: string;           // Марка автомобиля
    vehiclePlate: string;           // Государственный номер
    trailer: string;                // Номер прицепа
    driver: string;                 // ФИО водителя
    measurementsCount: number;      // Количество замеров
}

export interface CreateVehicleRequest {
    number: string;
    date: string;
    entryDate: string;
    exitDate?: string;
    counterparty: string;
    inn?: string | null;
    vehicleBrand: string;
    vehiclePlate: string;
    trailer: string;
    driver: string;
}

export interface UpdateVehicleRequest {
    number?: string;
    counterparty?: string;
    inn?: string | null;
    vehicleBrand?: string;
    vehiclePlate?: string;
    trailer?: string;
    driver?: string;
    exitDate?: string;
}

export interface MeasurementDto {
    id: string;
    vehicleId: string;
    vehicleNumber: string;
    vehiclePlate: string;
    humidityValue: number;
    temperatureC: number;
    measurementType: string | null;
    material: string | null;
    source: MeasurementSource;
    timestamp: string;
    sign: SignType;
    displayValue: string;
}

export interface CreateMeasurementRequest {
    vehicleId: string;
    humidityValue: number;
    temperatureC: number;
    measurementType: string | null;
    material: string | null;
    source: MeasurementSource;
    timestamp: string;
    sign: SignType;
}

export interface UpdateMeasurementRequest {
    humidityValue?: number;
    temperatureC?: number;
    measurementType?: string | null;
    material?: string | null;
    source?: MeasurementSource;
    sign?: SignType;
    timestamp?: string;
}

/**
 * Обёртка для пагинированного ответа от API.
 * @template T Тип элементов на странице.
 */
export interface PagedResult<T> {
    /** Элементы текущей страницы */
    items: T[];
    /** Общее количество записей */
    totalCount: number;
    /** Номер текущей страницы */
    pageNumber: number;
    /** Размер страницы */
    pageSize: number;
    /** Общее количество страниц */
    totalPages: number;
}

/**
 * Статистика по замерам для конкретной машины.
 */
export interface MeasurementStatisticsDto {
    /** Количество замеров */
    count: number;
    /** Средняя влажность */
    average: number | null;
    /** Минимальная влажность */
    min: number | null;
    /** Максимальная влажность */
    max: number | null;
    /** Дата и время последнего замера */
    lastMeasurementTimestamp: string | null;
    /** Количество ручных замеров */
    manualCount: number;
    /** Количество автоматических замеров */
    autoCount: number;
}