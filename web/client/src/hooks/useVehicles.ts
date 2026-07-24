import { useState, useEffect, useCallback } from 'react';
import { vehicleService } from '../services/api';
import type { VehicleDto, PagedResult } from '../types';

export const useVehicles = (pageNumber: number, pageSize: number) => {
    const [data, setData] = useState<PagedResult<VehicleDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const result = await vehicleService.getAll(pageNumber, pageSize);
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки данных'));
        } finally {
            setLoading(false);
        }
    }, [pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};