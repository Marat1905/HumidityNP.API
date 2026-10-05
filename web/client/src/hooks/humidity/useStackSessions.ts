import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { StackSessionDto, StackSessionsStatsDto } from '../../types/humidity';

/**
 * Хук для получения списка сессий работы со штабелями за период
 * И сводной статистики (сколько замеров попало в отчёт, а сколько — нет).
 *
 * Вся тяжёлая работа (оконные функции, склейка сессий, группировка поставщиков)
 * выполняется на сервере. Хук только нормализует границы периода
 * и параллельно запрашивает два эндпоинта:
 *  - /measurements/stack-sessions       — список сессий;
 *  - /measurements/stack-sessions-stats — сводка по «сырым» замерам.
 *
 * Статистика нужна, чтобы честно показать пользователю, почему цифры
 * в отчёте по штабелям могут отличаться от «Отчёта за период»:
 * замеры машин с пустым StackNumber в отчёт по штабелям не попадают.
 *
 * @param fromDate Начало периода (Date) или null.
 * @param toDate Конец периода (Date) или null.
 * @returns Объект с сессиями, статистикой, состоянием загрузки, ошибкой и refetch.
 */
export const useStackSessions = (
    fromDate: Date | null,
    toDate: Date | null
) => {
    const [data, setData] = useState<StackSessionDto[]>([]);
    const [stats, setStats] = useState<StackSessionsStatsDto | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchData = useCallback(async () => {
        if (!fromDate || !toDate) {
            setData([]);
            setStats(null);
            return;
        }

        // Нормализуем даты: from — начало дня (00:00 локально),
        // to — конец дня (23:59:59.999 локально).
        const from = new Date(fromDate);
        from.setHours(0, 0, 0, 0);
        const to = new Date(toDate);
        to.setHours(23, 59, 59, 999);

        const fromISO = from.toISOString();
        const toISO = to.toISOString();

        setLoading(true);
        setError(null);
        try {
            // Параллельно запрашиваем сессии и статистику — так быстрее,
            // чем последовательно, и статистика не блокирует показ сессий.
            const [sessions, statsResult] = await Promise.all([
                measurementService.getStackSessions(fromISO, toISO),
                measurementService.getStackSessionsStats(fromISO, toISO),
            ]);
            setData(sessions);
            setStats(statsResult);
        } catch (err: any) {
            setError(
                err instanceof Error
                    ? err
                    : new Error(err?.response?.data?.message || 'Ошибка загрузки сессий по штабелям')
            );
            setData([]);
            setStats(null);
        } finally {
            setLoading(false);
        }
    }, [fromDate, toDate]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, stats, loading, error, refetch: fetchData };
};