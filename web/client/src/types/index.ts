export enum MeasurementSource {
    Auto = 0,
    Manual = 1
}

export enum SignType {
    None = 0,
    Less = 1,
    Greater = 2
}

export interface VehicleDto {
    id: string;
    number: string;
    date: string;
    arrivalDate: string;
    entryDate: string;
    exitDate?: string;
    counterparty: string;
    workType: string;
    vehicleBrand: string;
    vehiclePlate: string;
    trailer: string;
    driver: string;
    loader: string;
    expeditor: string;
    department: string;
}

export interface CreateVehicleRequest {
    number: string;
    date: string;
    arrivalDate: string;
    entryDate: string;
    exitDate?: string;
    counterparty: string;
    workType: string;
    vehicleBrand: string;
    vehiclePlate: string;
    trailer: string;
    driver: string;
    loader: string;
    expeditor: string;
    department: string;
}

export interface UpdateVehicleRequest {
    number?: string;
    counterparty?: string;
    workType?: string;
    vehicleBrand?: string;
    vehiclePlate?: string;
    trailer?: string;
    driver?: string;
    loader?: string;
    expeditor?: string;
    department?: string;
    exitDate?: string;
}

export interface MeasurementDto {
    id: string;
    vehicleId: string;
    humidityValue: number;
    temperatureC: number;
    measurementType: string;
    material: string;
    source: MeasurementSource;
    timestamp: string;
    sign: SignType;
    displayValue: string;
}

export interface CreateMeasurementRequest {
    vehicleId: string;
    humidityValue: number;
    temperatureC: number;
    measurementType: string;
    material: string;
    source: MeasurementSource;
    timestamp: string;
    sign: SignType;
}

export interface UpdateMeasurementRequest {
    humidityValue?: number;
    temperatureC?: number;
    measurementType?: string;
    material?: string;
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