import { useState, useEffect, useCallback } from 'react';
import { supplierService } from '../services/api';
import type { SupplierDto, PagedResult } from '../types';

export const useSuppliers = (
    fromDate: Date | null,
    toDate: Date | null,
    pageNumber: number,
    pageSize: number
) => {
    const [data, setData] = useState<PagedResult<SupplierDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData(null);
            return;
        }
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
                pageSize
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки поставщиков'));
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};