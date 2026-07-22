import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../services/api';
import type { MeasurementDto, PagedResult } from '../types';

/**
 * Хук для получения пагинированного списка замеров в диапазоне дат.
 * @param fromDate Начало диапазона (Date) или null.
 * @param toDate Конец диапазона (Date) или null.
 * @param pageNumber Номер страницы.
 * @param pageSize Размер страницы.
 * @returns Объект с данными, состоянием загрузки и ошибкой.
 */
export const useMeasurementsByDateRange = (
    fromDate: Date | null,
    toDate: Date | null,
    pageNumber: number,
    pageSize: number
) => {
    const [data, setData] = useState<PagedResult<MeasurementDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData(null);
            setError(null);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            // Создаём копии дат и устанавливаем время:
            // from - начало дня (00:00:00.000) локально, to - конец дня (23:59:59.999) локально
            const from = new Date(fromDate);
            from.setHours(0, 0, 0, 0);
            const to = new Date(toDate);
            to.setHours(23, 59, 59, 999);

            // Преобразуем в UTC строки
            const fromISO = from.toISOString();
            const toISO = to.toISOString();

            const result = await measurementService.getByDateRange(fromISO, toISO, pageNumber, pageSize);
            setData(result);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Ошибка загрузки замеров по диапазону');
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};