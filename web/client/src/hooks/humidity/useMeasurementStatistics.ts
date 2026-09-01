import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { MeasurementStatisticsDto } from '../../types/humidity';

/**
 * Хук для получения статистики замеров для конкретной машины.
 * @param vehicleId Идентификатор машины или null.
 * @returns Объект с данными, состоянием загрузки и ошибкой.
 */
export const useMeasurementStatistics = (vehicleId: string | null) => {
    const [data, setData] = useState<MeasurementStatisticsDto | null>(null);
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
            const result = await measurementService.getStatisticsByVehicle(vehicleId);
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки статистики'));
        } finally {
            setLoading(false);
        }
    }, [vehicleId]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};