// src/hooks/useShiftReport.ts
import { useState, useEffect, useCallback } from 'react';
import { measurementService } from '../services/api';
import type { MeasurementDto } from '../types';

export type ShiftType = 'day' | 'night';

export interface ShiftReportItem {
    vehicleId: string;
    number: string;                // номер заявки
    vehiclePlate: string;          // госномер
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
 * Хук для получения отчёта по смене с общей статистикой.
 */
export const useShiftReport = (
    date: Date | null,
    shiftType: ShiftType,
    pageSize: number = 1000
) => {
    const [data, setData] = useState<ShiftReportData | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchReport = useCallback(async () => {
        if (!date) {
            setData(null);
            return;
        }

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

        const fromISO = shiftStart.toISOString();
        const toISO = shiftEnd.toISOString();

        setLoading(true);
        setError(null);

        try {
            const result = await measurementService.getByDateRange(fromISO, toISO, 1, pageSize);
            const measurements = result.items;

            // Агрегация по машинам
            const vehicleMap = new Map<string, {
                number: string;
                vehiclePlate: string;
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
                // При первом добавлении обновляем номер и госномер, если они ещё не заданы
                if (!entry.number && m.vehicleNumber) entry.number = m.vehicleNumber;
                if (!entry.vehiclePlate && m.vehiclePlate) entry.vehiclePlate = m.vehiclePlate;

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
                    measurementsCount: count,
                    averageHumidity: avg,
                    minHumidity: entry.minHumidity,
                    maxHumidity: entry.maxHumidity,
                    autoCount: entry.autoCount,
                    manualCount: entry.manualCount,
                    lastMeasurementTimestamp: entry.lastTimestamp,
                });
            }

            items.sort((a, b) => b.measurementsCount - a.measurementsCount);

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
            setError(err.response?.data?.message || 'Ошибка загрузки отчёта по смене');
        } finally {
            setLoading(false);
        }
    }, [date, shiftType, pageSize]);

    useEffect(() => {
        fetchReport();
    }, [fetchReport]);

    return { data, loading, error, refetch: fetchReport };
};