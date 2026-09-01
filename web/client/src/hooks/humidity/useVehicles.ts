import { useState, useEffect, useCallback, useRef } from 'react';
import { vehicleService } from '../../services/humidity/api';
import type { VehicleDto, PagedResult, VehiclesQueryParams } from '../../types/humidity';

export const useVehicles = (params: VehiclesQueryParams) => {
    const [data, setData] = useState<PagedResult<VehicleDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const prevParamsRef = useRef<string>('');

    const fetchData = useCallback(async () => {
        // Сериализуем параметры для сравнения
        const paramsKey = JSON.stringify(params);
        // Если параметры не изменились – пропускаем запрос (предотвращает лишние вызовы)
        if (prevParamsRef.current === paramsKey) {
            return;
        }
        prevParamsRef.current = paramsKey;

        setLoading(true);
        setError(null);
        try {
            const result = await vehicleService.getAll(params);
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки данных'));
        } finally {
            setLoading(false);
        }
    }, [params]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};