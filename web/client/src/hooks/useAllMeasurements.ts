import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../services/api';
import type { MeasurementDto, PagedResult } from '../types';

export const useAllMeasurements = (pageNumber: number, pageSize: number) => {
    const [data, setData] = useState<PagedResult<MeasurementDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const result = await measurementService.getAll(pageNumber, pageSize);
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки замеров'));
        } finally {
            setLoading(false);
        }
    }, [pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};