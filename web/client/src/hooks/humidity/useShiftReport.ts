import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../../services/humidity/api';
import type { MeasurementDto } from '../../types/humidity';

export type ShiftType = 'day' | 'night';

/**
 * Порядок сортировки отчёта по сменам.
 *  - 'exitDateDesc'           — по дате выезда машины, новые сверху (по умолчанию);
 *  - 'exitDateAsc'            — по дате выезда машины, старые сверху;
 *  - 'averageHumidityAsc'     — по средней влажности, ниже сверху (хорошие);
 *  - 'averageHumidityDesc'    — по средней влажности, выше сверху (плохие);
 *  - 'lastMeasurementDesc'    — по времени последнего замера, новые сверху.
 */
export type ShiftSortOrder =
    | 'exitDateDesc'
    | 'exitDateAsc'
    | 'averageHumidityAsc'
    | 'averageHumidityDesc'
    | 'lastMeasurementDesc';

/**
 * Элемент отчёта по смене (одна строка = одна машина).
 */
export interface ShiftReportItem {
    vehicleId: string;
    number: string; // номер заявки (пропуска)
    vehiclePlate: string; // госномер
    counterparty: string; // Поставщик
    /**
     * Дата въезда машины на площадку.
     * Отображается в отчёте и участвует в сортировке.
     */
    entryDate: string | null;
    /**
     * Дата выезда машины с площадки.
     * Именно по этому полю машина привязывается к смене.
     */
    exitDate: string | null;
    measurementsCount: number;
    averageHumidity: number | null;
    minHumidity: number | null;
    maxHumidity: number | null;
    autoCount: number;
    manualCount: number;
    lastMeasurementTimestamp: string | null;
}

/**
 * Общая статистика по смене.
 */
export interface ShiftSummaryStats {
    /** Количество машин, по которым есть замеры */
    vehicleCount: number;
    /** Общее количество замеров */
    totalMeasurements: number;
    /** Средняя влажность по всем замерам (взвешенная) */
    overallAverageHumidity: number | null;
    /** Минимальная влажность среди всех замеров */
    overallMinHumidity: number | null;
    /** Максимальная влажность среди всех замеров */
    overallMaxHumidity: number | null;
    /** Общее количество автоматических замеров */
    totalAutoCount: number;
    /** Общее количество ручных замеров */
    totalManualCount: number;
}

export interface ShiftReportData {
    shiftStart: Date;
    shiftEnd: Date;
    items: ShiftReportItem[];
    summary: ShiftSummaryStats;
}

/**
 * Хук для получения отчёта по смене.
 *
 * БИЗНЕС-ЛОГИКА ПРИВЯЗКИ МАШИНЫ К СМЕНЕ:
 * Машина относится к смене по времени выезда (Vehicle.ExitDate),
 * а не по времени отдельных замеров. Все замеры одной машины
 * попадают в ту смену, в которую машина фактически выехала с площадки.
 *
 * На сервер уходит диапазон [shiftStart, shiftEnd] в UTC; сервер возвращает
 * замеры для машин, у которых ExitDate попадает в этот диапазон.
 *
 * @param date Дата начала смены (в локальном времени пользователя).
 * @param shiftType Тип смены: 'day' (08:00–20:00) или 'night' (20:00–08:00).
 * @param pageSize Размер страницы. Отчёт по смене обычно требует ВСЕ замеры сразу,
 * поэтому значение по умолчанию — 20000 (максимум для API).
 * @param sortOrder Порядок сортировки. По умолчанию — по дате выезда, новые сверху.
 */
export const useShiftReport = (
    date: Date | null,
    shiftType: ShiftType,
    pageSize: number = 20000,
    sortOrder: ShiftSortOrder = 'exitDateDesc'
) => {
    const [data, setData] = useState<ShiftReportData | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const fetchReport = useCallback(async () => {
        if (!date) {
            setData(null);
            return;
        }

        // Вычисляем границы смены в ЛОКАЛЬНОМ времени пользователя.
        // Дневная смена: 08:00 – 20:00 текущего дня.
        // Ночная смена: 20:00 текущего дня – 08:00 следующего дня.
        const startOfDay = new Date(date);
        startOfDay.setHours(0, 0, 0, 0);

        let shiftStart: Date;
        let shiftEnd: Date;

        if (shiftType === 'day') {
            shiftStart = new Date(startOfDay);
            shiftStart.setHours(8, 0, 0, 0);
            shiftEnd = new Date(startOfDay);
            shiftEnd.setHours(20, 0, 0, 0);
        } else {
            shiftStart = new Date(startOfDay);
            shiftStart.setHours(20, 0, 0, 0);
            shiftEnd = new Date(startOfDay);
            shiftEnd.setDate(shiftEnd.getDate() + 1);
            shiftEnd.setHours(8, 0, 0, 0);
        }

        // Преобразуем локальные границы смены в UTC-строки для отправки на сервер.
        const fromISO = shiftStart.toISOString();
        const toISO = shiftEnd.toISOString();

        // Определяем порядок сортировки для сервера:
        // сервер поддерживает только 'asc'/'desc' по ExitDate.
        // Остальные сортировки (по средней влажности, по последнему замеру)
        // выполняем на клиенте после агрегации.
        const serverOrder: 'asc' | 'desc' =
            sortOrder === 'exitDateAsc' ? 'asc' : 'desc';

        setLoading(true);
        setError(null);

        try {
            const result = await measurementService.getByShift(
                fromISO,
                toISO,
                1,
                pageSize,
                serverOrder
            );

            // Безопасное получение массива замеров.
            const measurements = result?.items ?? [];

            // Агрегация по машинам.
            // Одна машина = одна строка отчёта, независимо от количества её замеров.
            const vehicleMap = new Map<string, {
                number: string;
                vehiclePlate: string;
                counterparty: string;
                entryDate: string | null;
                exitDate: string | null;
                measurements: MeasurementDto[];
                autoCount: number;
                manualCount: number;
                sumHumidity: number;
                minHumidity: number | null;
                maxHumidity: number | null;
                lastTimestamp: string | null;
            }>();

            let totalMeasurements = 0;
            let totalAuto = 0;
            let totalManual = 0;
            let sumAllHumidity = 0;
            let globalMin: number | null = null;
            let globalMax: number | null = null;

            measurements.forEach(m => {
                totalMeasurements++;
                if (m.source === 'Auto') totalAuto++;
                else totalManual++;

                sumAllHumidity += m.humidityValue;
                if (globalMin === null || m.humidityValue < globalMin) globalMin = m.humidityValue;
                if (globalMax === null || m.humidityValue > globalMax) globalMax = m.humidityValue;

                const id = m.vehicleId;
                if (!vehicleMap.has(id)) {
                    vehicleMap.set(id, {
                        number: m.vehicleNumber || '',
                        vehiclePlate: m.vehiclePlate || '',
                        counterparty: m.counterparty || '',
                        entryDate: m.vehicleEntryDate ?? null,
                        exitDate: m.vehicleExitDate ?? null,
                        measurements: [],
                        autoCount: 0,
                        manualCount: 0,
                        sumHumidity: 0,
                        minHumidity: null,
                        maxHumidity: null,
                        lastTimestamp: null,
                    });
                }

                const entry = vehicleMap.get(id)!;
                // При первом добавлении обновляем номер, госномер и поставщика, если они ещё не заданы
                if (!entry.number && m.vehicleNumber) entry.number = m.vehicleNumber;
                if (!entry.vehiclePlate && m.vehiclePlate) entry.vehiclePlate = m.vehiclePlate;
                if (!entry.counterparty && m.counterparty) entry.counterparty = m.counterparty;

                entry.measurements.push(m);
                if (m.source === 'Auto') entry.autoCount++;
                else entry.manualCount++;

                entry.sumHumidity += m.humidityValue;
                if (entry.minHumidity === null || m.humidityValue < entry.minHumidity) entry.minHumidity = m.humidityValue;
                if (entry.maxHumidity === null || m.humidityValue > entry.maxHumidity) entry.maxHumidity = m.humidityValue;

                if (!entry.lastTimestamp || m.timestamp > entry.lastTimestamp) {
                    entry.lastTimestamp = m.timestamp;
                }
            });

            const items: ShiftReportItem[] = [];
            for (const [vehicleId, entry] of vehicleMap.entries()) {
                const count = entry.measurements.length;
                const avg = count > 0 ? entry.sumHumidity / count : null;

                items.push({
                    vehicleId,
                    number: entry.number || vehicleId.slice(0, 8), // fallback на часть ID
                    vehiclePlate: entry.vehiclePlate || '—',
                    counterparty: entry.counterparty || '—',
                    entryDate: entry.entryDate,
                    exitDate: entry.exitDate,
                    measurementsCount: count,
                    averageHumidity: avg,
                    minHumidity: entry.minHumidity,
                    maxHumidity: entry.maxHumidity,
                    autoCount: entry.autoCount,
                    manualCount: entry.manualCount,
                    lastMeasurementTimestamp: entry.lastTimestamp,
                });
            }

            // Сортировка элементов отчёта в соответствии с выбранным порядком.
            // Для сортировки по дате выезда null-значения уходят в конец.
            const compareNullableDate = (a: string | null, b: string | null, desc: boolean): number => {
                if (a === null && b === null) return 0;
                if (a === null) return 1; // null всегда в конец
                if (b === null) return -1;
                const ta = new Date(a).getTime();
                const tb = new Date(b).getTime();
                return desc ? tb - ta : ta - tb;
            };

            items.sort((a, b) => {
                switch (sortOrder) {
                    case 'exitDateAsc':
                        return compareNullableDate(a.exitDate, b.exitDate, false);
                    case 'exitDateDesc':
                        return compareNullableDate(a.exitDate, b.exitDate, true);
                    case 'averageHumidityAsc':
                        // null-значения (нет замеров) уходят в конец
                        if (a.averageHumidity === null && b.averageHumidity === null) return 0;
                        if (a.averageHumidity === null) return 1;
                        if (b.averageHumidity === null) return -1;
                        return a.averageHumidity - b.averageHumidity;
                    case 'averageHumidityDesc':
                        if (a.averageHumidity === null && b.averageHumidity === null) return 0;
                        if (a.averageHumidity === null) return 1;
                        if (b.averageHumidity === null) return -1;
                        return b.averageHumidity - a.averageHumidity;
                    case 'lastMeasurementDesc':
                        return compareNullableDate(a.lastMeasurementTimestamp, b.lastMeasurementTimestamp, true);
                    default:
                        return 0;
                }
            });

            // Общая статистика
            const overallAverage = totalMeasurements > 0 ? sumAllHumidity / totalMeasurements : null;
            const summary: ShiftSummaryStats = {
                vehicleCount: vehicleMap.size,
                totalMeasurements,
                overallAverageHumidity: overallAverage,
                overallMinHumidity: globalMin,
                overallMaxHumidity: globalMax,
                totalAutoCount: totalAuto,
                totalManualCount: totalManual,
            };

            setData({
                shiftStart,
                shiftEnd,
                items,
                summary,
            });
        } catch (err: any) {
            setError(err instanceof Error ? err : new Error(err?.response?.data?.message || 'Ошибка загрузки отчёта по смене'));
        } finally {
            setLoading(false);
        }
    }, [date, shiftType, pageSize, sortOrder]);

    useEffect(() => {
        fetchReport();
    }, [fetchReport]);

    return { data, loading, error, refetch: fetchReport };
};