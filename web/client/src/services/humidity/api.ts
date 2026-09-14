import axios from 'axios';
import type {
    VehicleDto, CreateVehicleRequest, UpdateVehicleRequest,
    MeasurementDto, CreateMeasurementRequest, UpdateMeasurementRequest,
    PagedResult, MeasurementStatisticsDto,
    SupplierDetailsDto,
    SupplierDto,
    SupplierVehicleSummaryDto,
    PeriodReportResponseDto,
    PeriodReportSortBy,
    VehiclesQueryParams
} from '../../types/humidity';
import {
    requestInterceptor,
    requestErrorInterceptor,
    responseInterceptor,
    responseErrorInterceptor,
} from '../axiosInterceptors';

const API_BASE_URL = '/humidity/api/v1';

// Создаём экземпляр axios с базовым URL и общими заголовками
const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Применяем кастомные интерцепторы проекта
apiClient.interceptors.request.use(requestInterceptor, requestErrorInterceptor);
apiClient.interceptors.response.use(responseInterceptor, responseErrorInterceptor);

export const vehicleService = {
    // Изменяем метод getAll, добавляем параметры фильтрации
    async getAll(params: VehiclesQueryParams = {}): Promise<PagedResult<VehicleDto>> {
        const response = await apiClient.get('/vehicles', { params });
        return response.data;
    },
    async GetActive(pageNumber = 1, pageSize = 100): Promise<PagedResult<VehicleDto>> {
        const response = await apiClient.get('/vehicles/active', { params: { pageNumber, pageSize } });
        return response.data;
    },
    async getById(id: string): Promise<VehicleDto> {
        const response = await apiClient.get(`/vehicles/${id}`);
        return response.data;
    },
    async create(data: CreateVehicleRequest): Promise<VehicleDto> {
        const response = await apiClient.post('/vehicles', data);
        return response.data;
    },
    async update(id: string, data: UpdateVehicleRequest): Promise<VehicleDto> {
        const response = await apiClient.put(`/vehicles/${id}`, data);
        return response.data;
    },
    async delete(id: string): Promise<void> {
        await apiClient.delete(`/vehicles/${id}`);
    }
};

export const measurementService = {
    /**
    * Получить страницу всех замеров (без фильтра по машине)
    */
    async getAll(pageNumber = 1, pageSize = 20): Promise<PagedResult<MeasurementDto>> {
        const response = await apiClient.get('/measurements', { params: { pageNumber, pageSize } });
        return response.data;
    },
    /**
    * Получить страницу замеров в диапазоне дат (фильтр по Timestamp замера).
    * Используется в отчёте за период как «сырой» список (при необходимости).
    */
    async getByDateRange(
        from: string,
        to: string,
        pageNumber = 1,
        pageSize = 20
    ): Promise<PagedResult<MeasurementDto>> {
        const response = await apiClient.get('/measurements/range', {
            params: { from, to, pageNumber, pageSize }
        });
        return response.data;
    },
    /**
     * Получить страницу замеров для машин, у которых ВРЕМЯ ВЫЕЗДА (Vehicle.ExitDate)
     * попадает в указанный диапазон. Ключевой метод для отчёта по сменам:
     * все замеры машины относятся к той смене, в которую машина выехала.
     *
     * @param from Начало диапазона (ISO-строка) для времени выезда машины.
     * @param to Конец диапазона (ISO-строка) для времени выезда машины.
     * @param pageNumber Номер страницы (начиная с 1).
     * @param pageSize Размер страницы (макс. 20000).
     * @param order Порядок сортировки по дате выезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.
     */
    async getByShift(
        from: string,
        to: string,
        pageNumber = 1,
        pageSize = 20000,
        order: 'asc' | 'desc' = 'desc'
    ): Promise<PagedResult<MeasurementDto>> {
        const response = await apiClient.get('/measurements/shift', {
            params: { from, to, pageNumber, pageSize, order }
        });
        return response.data;
    },
    async getByVehicle(vehicleId: string, pageNumber = 1, pageSize = 100): Promise<PagedResult<MeasurementDto>> {
        const response = await apiClient.get(`/measurements/vehicle/${vehicleId}`, { params: { pageNumber, pageSize } });
        return response.data;
    },
    async getLatestByVehicle(vehicleId: string): Promise<MeasurementDto> {
        const response = await apiClient.get(`/measurements/vehicle/${vehicleId}/latest`);
        return response.data;
    },
    async getByDate(date: string, pageNumber = 1, pageSize = 20): Promise<PagedResult<MeasurementDto>> {
        const response = await apiClient.get(`/measurements/date/${date}`, { params: { pageNumber, pageSize } });
        return response.data;
    },
    async create(data: CreateMeasurementRequest): Promise<MeasurementDto> {
        const response = await apiClient.post('/measurements', data);
        return response.data;
    },
    async update(id: string, data: UpdateMeasurementRequest): Promise<MeasurementDto> {
        const response = await apiClient.put(`/measurements/${id}`, data);
        return response.data;
    },
    async delete(id: string): Promise<void> {
        await apiClient.delete(`/measurements/${id}`);
    },
    /**
     * Получить статистику по замерам для указанной машины.
     */
    async getStatisticsByVehicle(vehicleId: string): Promise<MeasurementStatisticsDto> {
        const response = await apiClient.get(`/measurements/vehicle/${vehicleId}/statistics`);
        return response.data;
    },

    /**
     * Получить агрегированный отчёт за период с сортировкой и пагинацией на сервере.
     *
     * Особенности:
     *  - сервер делает GROUP BY VehicleId, считает все агрегаты, сортирует и пагинирует в SQL;
     *  - на клиент уходит одна страница + общая статистика по всем машинам;
     *  - безопасно для длительных периодов (год и больше).
     *
     * @param from Начало периода (ISO-строка).
     * @param to Конец периода (ISO-строка).
     * @param sortBy Поле сортировки: 'exitDate' (по умолчанию), 'averageHumidity', 'lastMeasurement'.
     * @param order Порядок сортировки: 'desc' — по убыванию (по умолчанию), 'asc' — по возрастанию.
     * @param pageNumber Номер страницы (начиная с 1).
     * @param pageSize Размер страницы (макс. 500).
     */
    async getPeriodReport(
        from: string,
        to: string,
        sortBy: PeriodReportSortBy = 'exitDate',
        order: 'asc' | 'desc' = 'desc',
        pageNumber = 1,
        pageSize = 100
    ): Promise<PeriodReportResponseDto> {
        const response = await apiClient.get('/measurements/period-report', {
            params: { from, to, sortBy, order, pageNumber, pageSize }
        });
        return response.data;
    }
};

export const supplierService = {
    /**
     * Получить список поставщиков с агрегацией за период (пагинированный).
     * Используется наивная средняя влажность (AverageHumidity).
     */
    async getSuppliers(
        from: string,
        to: string,
        pageNumber = 1,
        pageSize = 20
    ): Promise<PagedResult<SupplierDto>> {
        const response = await apiClient.get('/suppliers', {
            params: { from, to, pageNumber, pageSize }
        });
        return response.data;
    },

    /**
     * Получить детальную информацию по поставщику за период.
     * Пагинация и сортировка выполняются на стороне сервера.
     *
     * @param inn ИНН поставщика.
     * @param from Начало периода (ISO-строка).
     * @param to Конец периода (ISO-строка).
     * @param pageNumber Номер страницы (начиная с 1).
     * @param pageSize Размер страницы (макс. 100).
     * @param order Порядок сортировки по дате въезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.
     */
    async getSupplierDetails(
        inn: string,
        from: string,
        to: string,
        pageNumber = 1,
        pageSize = 10,
        order: 'asc' | 'desc' = 'desc'
    ): Promise<SupplierDetailsDto> {
        const response = await apiClient.get(`/suppliers/${inn}/details`, {
            params: { from, to, pageNumber, pageSize, order }
        });
        return response.data;
    },

    /**
     * Получить полный (без пагинации) список машин поставщика за период,
     * отсортированный по дате въезда. Используется для построения графика.
     *
     * @param inn ИНН поставщика.
     * @param from Начало периода (ISO-строка).
     * @param to Конец периода (ISO-строка).
     * @param order Порядок сортировки по дате въезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.
     */
    async getSupplierVehiclesForChart(
        inn: string,
        from: string,
        to: string,
        order: 'asc' | 'desc' = 'desc'
    ): Promise<SupplierVehicleSummaryDto[]> {
        const response = await apiClient.get(`/suppliers/${inn}/chart`, {
            params: { from, to, order }
        });
        return response.data;
    },

    /**
     * Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
     *
     * @param from Начало периода (ISO-строка).
     * @param to Конец периода (ISO-строка).
     * @param top Количество записей в топе.
     * @param order 'asc' — хорошие (низкая влажность), 'desc' — плохие (высокая).
     * @param priorWeight Вес prior (C) для байесовской коррекции. По умолчанию 30.
     *                    Значения: 0 — без коррекции, 10 — слабая, 30 — умеренная, 100 — сильная.
     */
    async getTopSuppliers(
        from: string,
        to: string,
        top = 10,
        order: 'asc' | 'desc' = 'asc',
        priorWeight = 30
    ): Promise<SupplierDto[]> {
        const response = await apiClient.get('/suppliers/top', {
            params: { from, to, top, order, priorWeight }
        });
        return response.data;
    }
};

export default apiClient;