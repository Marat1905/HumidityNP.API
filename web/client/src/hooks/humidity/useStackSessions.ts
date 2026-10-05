import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { StackSessionDto } from '../../types/humidity';

/**
 * Хук для получения списка сессий работы со штабелями за период.
 *
 * Вся тяжёлая работа (оконные функции, склейка сессий, группировка поставщиков)
 * выполняется на сервере одним SQL-запросом. Хук только нормализует границы
 * периода и прокидывает их в API.
 *
 * @param fromDate Начало периода (Date) или null.
 * @param toDate Конец периода (Date) или null.
 * @returns Объект с данными, состоянием загрузки, ошибкой и функцией повторной загрузки.
 */
export const useStackSessions = (
    fromDate: Date | null,
    toDate: Date | null
) => {
    const [data, setData] = useState<StackSessionDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData([]);
            return;
        }

        // Нормализуем даты: from — начало дня (00:00 локально),
        // to — конец дня (23:59:59.999 локально).
        // Дальше toISOString() даст UTC-строку для отправки на сервер.
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        setLoading(true);
        setError(null);
        try {
            const result = await measurementService.getStackSessions(
                from.toISOString(),
                to.toISOString()
            );
            setData(result);
        } catch (err: any) {
            setError(
                err instanceof Error
                    ? err
                    : new Error(err?.response?.data?.message || 'Ошибка загрузки сессий по штабелям')
            );
            setData([]);
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};