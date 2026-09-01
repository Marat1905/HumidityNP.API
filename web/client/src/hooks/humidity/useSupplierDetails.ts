import { useState, useEffect, useCallback } from 'react';
import { supplierService } from '../../services/humidity/api';
import type { SupplierDetailsDto } from '../../types/humidity';

export const useSupplierDetails = (
    inn: string | null,
    fromDate: Date | null,
    toDate: Date | null
) => {
    const [data, setData] = useState<SupplierDetailsDto | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!inn || !fromDate || !toDate) {
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
            const result = await supplierService.getSupplierDetails(
                inn,
                from.toISOString(),
                to.toISOString()
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки деталей поставщика'));
            setData(null);
        } finally {
            setLoading(false);
        }
    }, [inn, fromDate, toDate]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};