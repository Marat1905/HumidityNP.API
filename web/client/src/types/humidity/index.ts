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
    number: string; // Номер пропуска
    date: string; // Дата создания пропуска
    entryDate: string; // Дата въезда на площадку
    exitDate?: string; // Дата выезда с площадки
    counterparty: string; // Поставщик
    inn?: string | null; // ИНН поставщика
    vehicleBrand: string; // Марка автомобиля
    vehiclePlate: string; // Государственный номер
    trailer: string; // Номер прицепа
    driver: string; // ФИО водителя
    measurementsCount: number; // Количество замеров
    /** Количество тюков, выгруженных из машины */
    baleCount?: number | null;
    /** Количество порванных тюков */
    damagedBaleCount?: number | null;
    /** Вес выгруженного груза в килограммах */
    weightKg?: number | null;
    /** Номер штабеля, куда выгружена машина */
    stackNumber?: string | null;
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
    counterparty?: string; // Поставщик для отображения в отчетах
    /**
     * Дата въезда машины на площадку (из сущности Vehicle).
     * Используется для отображения в отчётах и сортировки.
     */
    vehicleEntryDate?: string | null;
    /**
     * Дата выезда машины с площадки (из сущности Vehicle).
     * Может быть null, если машина ещё не выехала.
     */
    vehicleExitDate?: string | null;
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

// ===== Поставщики =====

export interface SupplierDto {
    inn: string;
    counterparty: string;
    vehiclesCount: number;
    measuredVehiclesCount: number;
    totalMeasurements: number;
    /**
     * Наивная средняя влажность (sum / count).
     * Используется в обычном списке поставщиков и как справочное значение в топе.
     */
    averageHumidity: number | null;
    /**
     * Байесовски скорректированная средняя влажность.
     * Заполняется ТОЛЬКО для топа поставщиков; в обычном списке — null.
     * Формула: (C * m + sum) / (C + n), где
     *   C — priorWeight, m — globalAverageHumidity, sum/n — агрегаты поставщика.
     */
    adjustedAverageHumidity: number | null;
    /** Вес prior (C), использованный при коррекции. 0 — коррекция не применялась. */
    priorWeight: number;
    /** Глобальная средняя влажность по всем замерам за период (prior mean m). */
    globalAverageHumidity: number | null;
    minHumidity: number | null;
    maxHumidity: number | null;
}

export interface SupplierVehicleSummaryDto {
    vehicleId: string;
    number: string;
    vehiclePlate: string;
    entryDate: string;
    exitDate?: string | null;
    measurementsCount: number;
    averageHumidity: number | null;
    minHumidity: number | null;
    maxHumidity: number | null;
    autoCount: number;
    manualCount: number;
    lastMeasurementTimestamp: string | null;
}

/**
 * Детальная информация по поставщику.
 * Пагинация и сортировка списка машин выполняются на стороне сервера.
 * Поле overallStatistics содержит статистику по ВСЕМ машинам за период,
 * а не только по текущей странице.
 */
export interface SupplierDetailsDto {
    inn: string;
    counterparty: string;
    vehicles: PagedResult<SupplierVehicleSummaryDto>;
    overallStatistics: MeasurementStatisticsDto;
}

/**
 * Параметры запроса списка машин.
 * Используются на странице «Машины» для фильтрации и пагинации.
 */
export interface VehiclesQueryParams {
    pageNumber?: number;
    pageSize?: number;
    counterparty?: string;
    status?: 'active' | 'exited' | 'all';
    plate?: string;
    driver?: string;
    /**
     * Минимальная дата въезда (включительно), ISO-строка.
     * Если не задана — фильтр по нижней границе не применяется.
     */
    entryDateFrom?: string;
    /**
     * Максимальная дата въезда (включительно), ISO-строка.
     * Если не задана — фильтр по верхней границе не применяется.
     */
    entryDateTo?: string;
}

// ===== Отчёт за период =====

/**
 * Одна строка отчёта за период: агрегированные данные по одной машине.
 * Формируется на сервере одним SQL-запросом с группировкой по VehicleId.
 */
export interface PeriodReportItemDto {
    vehicleId: string;
    number: string;
    vehiclePlate: string;
    counterparty: string;
    entryDate: string | null;
    exitDate: string | null;
    measurementsCount: number;
    averageHumidity: number | null;
    minHumidity: number | null;
    maxHumidity: number | null;
    autoCount: number;
    manualCount: number;
    lastMeasurementTimestamp: string | null;
}

/**
 * Общая статистика по всем машинам за период.
 * Считается на сервере по полному набору данных (не зависит от страницы).
 */
export interface PeriodReportSummaryDto {
    vehicleCount: number;
    totalMeasurements: number;
    overallAverageHumidity: number | null;
    overallMinHumidity: number | null;
    overallMaxHumidity: number | null;
    totalAutoCount: number;
    totalManualCount: number;
}

/**
 * Полный ответ отчёта за период: постраничный список машин и общая статистика.
 */
export interface PeriodReportResponseDto {
    vehicles: PagedResult<PeriodReportItemDto>;
    summary: PeriodReportSummaryDto;
}

/**
 * Поле сортировки отчёта за период.
 * Соответствует серверному параметру sortBy.
 */
export type PeriodReportSortBy = 'exitDate' | 'averageHumidity' | 'lastMeasurement';

/**
 * DTO с информацией о версии бэкенда.
 * Соответствует VersionResponse из VersionController на сервере (Humidity.API).
 */
export interface VersionResponse {
    /** Имя приложения (сборки) */
    applicationName: string;
    /** Версия приложения (из APP_VERSION или версии сборки .NET) */
    version: string;
    /** Окружение (Development / Production и т.п.) */
    environment: string;
    /** Хэш коммита Git (из GIT_COMMIT) */
    gitCommit: string;
    /** Дата сборки бинарника */
    buildDate: string;
}