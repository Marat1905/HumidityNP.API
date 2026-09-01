import axios from 'axios';
import type {
    VehicleDto, CreateVehicleRequest, UpdateVehicleRequest,
    MeasurementDto, CreateMeasurementRequest, UpdateMeasurementRequest,
    PagedResult, MeasurementStatisticsDto,
    SupplierDetailsDto,
    SupplierDto,
    VehiclesQueryParams
} from '../../types/humidity';

const API_BASE_URL = '/humidity/api/v1';

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

apiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('access_token');
    if (token) {
        config.headers = config.headers || {};
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            localStorage.removeItem('access_token');
            window.location.href = '/login';
        }
        return Promise.reject(error);
    }
);

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
    * Получить страницу замеров в диапазоне дат
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
    }
};

export const supplierService = {
    /**
     * Получить список поставщиков с агрегацией за период.
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
     */
    async getSupplierDetails(
        inn: string,
        from: string,
        to: string
    ): Promise<SupplierDetailsDto> {
        const response = await apiClient.get(`/suppliers/${inn}/details`, {
            params: { from, to }
        });
        return response.data;
    },
    /**
     * Получить топ-N поставщиков по средней влажности за период.
     * @param from Начало периода (ISO-строка).
     * @param to Конец периода (ISO-строка).
     * @param top Количество записей.
     * @param order 'asc' — хорошие (низкая влажность), 'desc' — плохие (высокая).
     */
    async getTopSuppliers(
        from: string,
        to: string,
        top = 10,
        order: 'asc' | 'desc' = 'asc'
    ): Promise<SupplierDto[]> {
        const response = await apiClient.get('/suppliers/top', {
            params: { from, to, top, order }
        });
        return response.data;
    }
};

export default apiClient;