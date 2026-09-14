import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type {
    PeriodReportResponseDto,
    PeriodReportSortBy,
} from '../../types/humidity';

/**
 * Хук для получения агрегированного отчёта за период с сервера.
 *
 * Сервер сам делает GROUP BY VehicleId, сортировку и пагинацию в SQL,
 * поэтому хук не выполняет никакой агрегации или сортировки на клиенте —
 * он просто возвращает то, что вернул сервер.
 *
 * Это позволяет безопасно запрашивать отчёты за длительные периоды
 * (год и больше) без выгрузки всех замеров на клиент.
 *
 * @param fromDate Начало периода (Date) или null.
 * @param toDate Конец периода (Date) или null.
 * @param sortBy Поле сортировки: 'exitDate' (по умолчанию), 'averageHumidity', 'lastMeasurement'.
 * @param order Порядок сортировки: 'desc' — по убыванию (по умолчанию), 'asc' — по возрастанию.
 * @param pageNumber Номер страницы (начиная с 1).
 * @param pageSize Размер страницы.
 * @returns Объект с данными, состоянием загрузки, ошибкой и функцией повторной загрузки.
 */
export const usePeriodReport = (
    fromDate: Date | null,
    toDate: Date | null,
    sortBy: PeriodReportSortBy = 'exitDate',
    order: 'asc' | 'desc' = 'desc',
    pageNumber: number = 1,
    pageSize: number = 100
) => {
    const [data, setData] = useState<PeriodReportResponseDto | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData(null);
            return;
        }

        // Нормализуем даты: from — начало дня (00:00 локально),
        // to — конец дня (23:59:59.999 локально).
        // Затем преобразуем в UTC-ISO для отправки на сервер.
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        const fromISO = from.toISOString();
        const toISO = to.toISOString();

        setLoading(true);
        setError(null);

        try {
            const result = await measurementService.getPeriodReport(
                fromISO,
                toISO,
                sortBy,
                order,
                pageNumber,
                pageSize
            );
            setData(result);
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки отчёта за период'));
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate, sortBy, order, pageNumber, pageSize]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData };
};