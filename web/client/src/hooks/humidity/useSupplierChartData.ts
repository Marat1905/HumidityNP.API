import { useState, useEffect, useCallback } from 'react';
import { supplierService } from '../../services/humidity/api';
import type { SupplierVehicleSummaryDto } from '../../types/humidity';

/**
 * Хук для получения полного (без пагинации) списка машин поставщика за период,
 * отсортированного по дате въезда. Используется для построения графика.
 * Сортировка выполняется на стороне сервера, пагинация НЕ применяется.
 *
 * @param inn ИНН поставщика или null.
 * @param fromDate Начало периода или null.
 * @param toDate Конец периода или null.
 * @param order Порядок сортировки по дате въезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.
 */
export const useSupplierChartData = (
    inn: string | null,
    fromDate: Date | null,
    toDate: Date | null,
    order: 'asc' | 'desc' = 'desc'
) => {
    const [data, setData] = useState<SupplierVehicleSummaryDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!inn || !fromDate || !toDate) {
            setData([]);
            return;
        }
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        setLoading(true);
        setError(null);
        try {
            const result = await supplierService.getSupplierVehiclesForChart(
                inn,
                from.toISOString(),
                to.toISOString(),
                order
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки данных для графика'));
            setData([]);
        } finally {
            setLoading(false);
        }
    }, [inn, fromDate, toDate, order]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};