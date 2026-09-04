import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { MeasurementDto } from '../../types/humidity';

/**
 * Хук для получения ВСЕХ замеров в диапазоне дат (обходит пагинацию).
 * @param fromDate Начало диапазона (Date) или null.
 * @param toDate Конец диапазона (Date) или null.
 * @param maxPageSize Размер страницы при запросе (по умолчанию 100, максимально допустимое API).
 * @returns Объект с массивом замеров, состоянием загрузки и ошибкой.
 */
export const useAllMeasurementsByDateRange = (
    fromDate: Date | null,
    toDate: Date | null,
    maxPageSize: number = 100
) => {
    const [measurements, setMeasurements] = useState<MeasurementDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchAll = useCallback(async () => {
        if (!fromDate || !toDate) {
            setMeasurements([]);
            setError(null);
            return;
        }

        // Нормализуем даты: from – начало дня (00:00), to – конец дня (23:59:59)
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        const fromISO = from.toISOString();
        const toISO = to.toISOString();

        setLoading(true);
        setError(null);

        try {
            let allItems: MeasurementDto[] = [];
            let currentPage = 1;
            let totalPages = 1;

            // Цикл по страницам, пока не получим все
            do {
                const result = await measurementService.getByDateRange(
                    fromISO,
                    toISO,
                    currentPage,
                    maxPageSize
                );

                // ИСПРАВЛЕНИЕ: безопасное получение массива items для предотвращения добавления undefined
                allItems = allItems.concat(result?.items ?? []);
                totalPages = result?.totalPages ?? 1;
                currentPage++;
            } while (currentPage <= totalPages);

            setMeasurements(allItems);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки замеров за период'));
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, maxPageSize]);

    useEffect(() => {
        fetchAll();
    }, [fetchAll]);

    return { measurements, loading, error, refetch: fetchAll };
};