import { useState, useEffect, useCallback } from 'react';
import { supplierService } from '../../services/humidity/api';
import type { SupplierDto } from '../../types/humidity';

/**
 * Хук для получения топ-поставщиков по средней влажности с байесовской коррекцией.
 *
 * @param fromDate Начало периода (Date) или null.
 * @param toDate Конец периода (Date) или null.
 * @param top Количество записей (макс. 100).
 * @param order 'asc' — хорошие (низкая влажность), 'desc' — плохие (высокая).
 * @param priorWeight Вес prior (C) для байесовской коррекции:
 *                    0 — без коррекции (наивная средняя);
 *                    10 — слабая;
 *                    30 — умеренная (по умолчанию);
 *                    100 — сильная.
 * @returns Объект с данными, состоянием загрузки и ошибкой.
 */
export const useTopSuppliers = (
    fromDate: Date | null,
    toDate: Date | null,
    top: number = 10,
    order: 'asc' | 'desc' = 'asc',
    priorWeight: number = 30
) => {
    const [data, setData] = useState<SupplierDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData([]);
            setError(null);
            return;
        }
        // Нормализуем даты
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        setLoading(true);
        setError(null);
        try {
            const result = await supplierService.getTopSuppliers(
                from.toISOString(),
                to.toISOString(),
                top,
                order,
                priorWeight
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки топ-поставщиков'));
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, top, order, priorWeight]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};