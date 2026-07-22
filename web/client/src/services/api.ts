import axios from 'axios';
import type { 
  VehicleDto, CreateVehicleRequest, UpdateVehicleRequest,
  MeasurementDto, CreateMeasurementRequest, UpdateMeasurementRequest,
  PagedResult 
} from '../types';

const API_BASE_URL = '/api/v1';

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
  async getAll(pageNumber = 1, pageSize = 100): Promise<PagedResult<VehicleDto>> {
    const response = await apiClient.get('/vehicles', { params: { pageNumber, pageSize } });
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
  }
};

export default apiClient;