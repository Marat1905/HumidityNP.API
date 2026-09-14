import { useState, useEffect, useCallback } from 'react';
import { supplierService } from '../../services/humidity/api';
import type { SupplierDto, PagedResult } from '../../types/humidity';

/**
 * Хук для получения списка поставщиков с пагинацией и поиском.
 *
 * @param fromDate Начало периода (Date) или null.
 * @param toDate Конец периода (Date) или null.
 * @param pageNumber Номер страницы (начиная с 1).
 * @param pageSize Размер страницы.
 * @param search Строка поиска по ИНН или наименованию поставщика.
 *               Пустая строка или undefined — поиск не применяется.
 * @returns Объект с данными, состоянием загрузки, ошибкой и функцией повторной загрузки.
 */
export const useSuppliers = (
    fromDate: Date | null,
    toDate: Date | null,
    pageNumber: number,
    pageSize: number,
    search?: string
) => {
    const [data, setData] = useState<PagedResult<SupplierDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData(null);
            return;
        }
        // Нормализуем даты: from — начало дня (00:00), to — конец дня (23:59:59.999).
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        setLoading(true);
        setError(null);
        try {
            const result = await supplierService.getSuppliers(
                from.toISOString(),
                to.toISOString(),
                pageNumber,
                pageSize,
                search
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки поставщиков'));
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, pageNumber, pageSize, search]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};