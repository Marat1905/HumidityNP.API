import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { MeasurementDto, PagedResult } from '../../types/humidity';

export const useMeasurements = (vehicleId: string | null, pageNumber: number, pageSize: number) => {
    const [data, setData] = useState<PagedResult<MeasurementDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!vehicleId) {
            setData(null);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const result = await measurementService.getByVehicle(vehicleId, pageNumber, pageSize);
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки замеров'));
        } finally {
            setLoading(false);
        }
    }, [vehicleId, pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};