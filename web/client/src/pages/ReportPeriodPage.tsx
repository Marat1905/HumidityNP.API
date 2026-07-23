// src/pages/ReportPeriodPage.tsx
import { useState, useEffect, useMemo } from 'react';
import { format, subDays } from 'date-fns';
import { ru } from 'date-fns/locale';
import { useAllMeasurementsByDateRange } from '../hooks/useAllMeasurementsByDateRange';
import RangeDatePicker from '../components/RangeDatePicker';
import PeriodReportTable, { type PeriodReportItem, type PeriodSummaryStats } from '../components/PeriodReportTable';
import PeriodReportCardView from '../components/PeriodReportCardView';
import Spinner from '../components/Spinner';
import { MeasurementSource } from '../types';
import { LayoutGrid, Table } from 'lucide-react';

type ViewMode = 'table' | 'cards';

export default function ReportPeriodPage() {
    // Состояние диапазона дат (по умолчанию последние 7 дней)
    const [startDate, setStartDate] = useState<Date | null>(() => {
        const now = new Date();
        return subDays(now, 6);
    });
    const [endDate, setEndDate] = useState<Date | null>(() => new Date());

    const [viewMode, setViewMode] = useState<ViewMode>('table');

    // Обработчик изменения диапазона из RangeDatePicker
    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        setStartDate(start);
        setEndDate(end);
    };

    // Загружаем все замеры за период
    const { measurements, loading, error, refetch } = useAllMeasurementsByDateRange(
        startDate,
        endDate,
        100 // максимальный pageSize
    );

    // При изменении дат перезапрашиваем
    useEffect(() => {
        refetch();
    }, [startDate, endDate, refetch]);

    // Агрегация данных по машинам (такая же, как была)
    const reportData = useMemo(() => {
        if (!measurements || measurements.length === 0) {
            return { items: [] as PeriodReportItem[], summary: null as PeriodSummaryStats | null };
        }

        const vehicleMap = new Map<string, {
            measurements: typeof measurements;
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
            if (m.source === MeasurementSource.Auto) totalAuto++;
            else totalManual++;
            sumAllHumidity += m.humidityValue;
            if (globalMin === null || m.humidityValue < globalMin) globalMin = m.humidityValue;
            if (globalMax === null || m.humidityValue > globalMax) globalMax = m.humidityValue;

            const id = m.vehicleId;
            if (!vehicleMap.has(id)) {
                vehicleMap.set(id, {
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
            entry.measurements.push(m);
            if (m.source === MeasurementSource.Auto) entry.autoCount++;
            else entry.manualCount++;
            entry.sumHumidity += m.humidityValue;
            if (entry.minHumidity === null || m.humidityValue < entry.minHumidity) entry.minHumidity = m.humidityValue;
            if (entry.maxHumidity === null || m.humidityValue > entry.maxHumidity) entry.maxHumidity = m.humidityValue;
            if (!entry.lastTimestamp || m.timestamp > entry.lastTimestamp) {
                entry.lastTimestamp = m.timestamp;
            }
        });

        const items: PeriodReportItem[] = [];
        for (const [vehicleId, entry] of vehicleMap.entries()) {
            const count = entry.measurements.length;
            const avg = count > 0 ? entry.sumHumidity / count : null;
            items.push({
                vehicleId,
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

        const overallAverage = totalMeasurements > 0 ? sumAllHumidity / totalMeasurements : null;

        const summary: PeriodSummaryStats = {
            vehicleCount: vehicleMap.size,
            totalMeasurements,
            overallAverageHumidity: overallAverage,
            overallMinHumidity: globalMin,
            overallMaxHumidity: globalMax,
            totalAutoCount: totalAuto,
            totalManualCount: totalManual,
        };

        return { items, summary };
    }, [measurements]);

    // Формирование строки с периодом для отображения
    const periodLabel = useMemo(() => {
        if (!startDate || !endDate) return 'не выбран';
        const fromStr = format(startDate, 'dd.MM.yyyy');
        const toStr = format(endDate, 'dd.MM.yyyy');
        return `с ${fromStr} по ${toStr}`;
    }, [startDate, endDate]);

    if (loading) return <Spinner />;
    if (error) return <div className="text-red-500 text-center py-10">{error}</div>;

    return (
        <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Отчёт за период
            </h2>

            {/* Панель выбора периода и переключатель вида */}
            <div className="flex flex-wrap items-center gap-4 mb-6 p-4 bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Период:</span>
                    <div className="w-64">
                        <RangeDatePicker
                            startDate={startDate}
                            endDate={endDate}
                            onChange={handleDateRangeChange}
                            size="md"
                        />
                    </div>
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                    {periodLabel}
                </div>

                <div className="ml-auto flex items-center gap-2">
                    <span className="text-sm text-gray-500 dark:text-gray-400 mr-1">Вид:</span>
                    <button
                        onClick={() => setViewMode('table')}
                        className={`p-2 rounded-lg border transition ${viewMode === 'table'
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                            : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                            }`}
                        aria-label="Табличный вид"
                    >
                        <Table className="w-5 h-5" />
                    </button>
                    <button
                        onClick={() => setViewMode('cards')}
                        className={`p-2 rounded-lg border transition ${viewMode === 'cards'
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                            : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                            }`}
                        aria-label="Карточный вид"
                    >
                        <LayoutGrid className="w-5 h-5" />
                    </button>
                </div>
            </div>

            {/* Отображение отчёта в выбранном виде */}
            {reportData.items.length === 0 ? (
                <div className="text-center py-10 text-gray-500 dark:text-gray-400">
                    Нет данных за выбранный период.
                </div>
            ) : (
                <>
                    {viewMode === 'table' ? (
                        <PeriodReportTable
                            items={reportData.items}
                            summary={reportData.summary!}
                            periodLabel={periodLabel}
                        />
                    ) : (
                        <PeriodReportCardView
                            items={reportData.items}
                            summary={reportData.summary!}
                            periodLabel={periodLabel}
                        />
                    )}
                </>
            )}
        </div>
    );
}