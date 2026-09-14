// src/pages/ReportPeriodPage.tsx

import { useState, useEffect, useMemo } from 'react';
import { format, subDays } from 'date-fns';
import { ru } from 'date-fns/locale';
import { useAllMeasurementsByDateRange } from '../../hooks/humidity';
import { SkeletonReport, RangeDatePicker } from '../../components/common';
import { PeriodReportCardView, PeriodReportTable, type PeriodReportItem, type PeriodSummaryStats } from '../../components/humidity';
import { MeasurementSource, type MeasurementDto } from '../../types/humidity';
import { LayoutGrid, Table, RotateCcw } from 'lucide-react';

type ViewMode = 'table' | 'cards';

/**
 * Порядок сортировки отчёта за период.
 * Пользователь выбирает его в выпадающем списке в панели фильтров.
 *
 * Варианты «По замеров» (measurementsCountDesc / measurementsCountAsc) удалены.
 * Значение по умолчанию — 'exitDateDesc' (по дате выезда машины, новые сверху).
 * Это согласуется с бизнес-логикой привязки машины к смене: ключевое событие —
 * выезд машины с площадки, поэтому и в отчёте за период логично сортировать
 * по времени выезда, чтобы последние выехавшие машины были вверху списка.
 */
type PeriodSortOrder =
    | 'exitDateDesc'
    | 'exitDateAsc'
    | 'averageHumidityAsc'
    | 'averageHumidityDesc'
    | 'lastMeasurementDesc';

/**
 * Значение сортировки по умолчанию.
 * Вынесено в константу, чтобы использовать и в useState, и в resetFilter —
 * это исключает рассинхронизацию «стартовое значение» / «значение сброса».
 */
const DEFAULT_SORT_ORDER: PeriodSortOrder = 'exitDateDesc';

export default function ReportPeriodPage() {
    // Состояние диапазона дат (по умолчанию последние 7 дней)
    const [startDate, setStartDate] = useState<Date | null>(() => {
        const now = new Date();
        return subDays(now, 6);
    });

    const [endDate, setEndDate] = useState<Date | null>(() => new Date());
    const [viewMode, setViewMode] = useState<ViewMode>('table');

    // Порядок сортировки отчёта.
    // По умолчанию — по дате выезда машины, новые сверху (exitDateDesc).
    const [sortOrder, setSortOrder] = useState<PeriodSortOrder>(DEFAULT_SORT_ORDER);

    // Обработчик изменения диапазона из RangeDatePicker
    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        setStartDate(start);
        setEndDate(end);
    };

    // Сброс фильтра – возвращаем к диапазону по умолчанию (последние 7 дней)
    // и порядку сортировки по умолчанию (по дате выезда, новые сверху).
    const resetFilter = () => {
        const now = new Date();
        setStartDate(subDays(now, 6));
        setEndDate(now);
        setSortOrder(DEFAULT_SORT_ORDER);
    };

    // Загружаем все замеры за период
    const { measurements, loading, error, refetch } = useAllMeasurementsByDateRange(
        startDate,
        endDate,
        100 // максимальный pageSize на запрос (при необходимости хук сам обходит пагинацию)
    );

    // При изменении дат перезапрашиваем
    useEffect(() => {
        refetch();
    }, [startDate, endDate, refetch]);

    // Агрегация данных по машинам
    const reportData = useMemo(() => {
        // ИСПРАВЛЕНИЕ: фильтруем массив, убирая возможные undefined/null элементы перед обработкой
        const validMeasurements = (measurements ?? []).filter((m): m is MeasurementDto => m != null);

        if (validMeasurements.length === 0) {
            return { items: [] as PeriodReportItem[], summary: null as PeriodSummaryStats | null };
        }

        const vehicleMap = new Map<string, {
            number: string;
            vehiclePlate: string;
            counterparty: string;
            entryDate: string | null;
            exitDate: string | null;
            measurements: typeof validMeasurements;
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

        validMeasurements.forEach(m => {
            totalMeasurements++;
            if (m.source === MeasurementSource.Auto) totalAuto++;
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
            if (!entry.number && m.vehicleNumber) entry.number = m.vehicleNumber;
            if (!entry.vehiclePlate && m.vehiclePlate) entry.vehiclePlate = m.vehiclePlate;
            if (!entry.counterparty && m.counterparty) entry.counterparty = m.counterparty;

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
                number: entry.number || vehicleId.slice(0, 8),
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
        // Для сортировки по датам null-значения уходят в конец.
        const compareNullableDate = (a: string | null, b: string | null, desc: boolean): number => {
            if (a === null && b === null) return 0;
            if (a === null) return 1;
            if (b === null) return -1;
            const ta = new Date(a).getTime();
            const tb = new Date(b).getTime();
            return desc ? tb - ta : ta - tb;
        };

        items.sort((a, b) => {
            switch (sortOrder) {
                case 'averageHumidityAsc':
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
                case 'exitDateAsc':
                    return compareNullableDate(a.exitDate ?? null, b.exitDate ?? null, false);
                case 'exitDateDesc':
                    return compareNullableDate(a.exitDate ?? null, b.exitDate ?? null, true);
                default:
                    return 0;
            }
        });

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
    }, [measurements, sortOrder]);

    // Формирование строки с периодом для отображения
    const periodLabel = useMemo(() => {
        if (!startDate || !endDate) return 'не выбран';
        const fromStr = format(startDate, 'dd.MM.yyyy');
        const toStr = format(endDate, 'dd.MM.yyyy');
        return `с ${fromStr} по ${toStr}`;
    }, [startDate, endDate]);

    // --- Обработка состояний ---
    // 1. Загрузка
    if (loading) return <SkeletonReport />;

    // 2. Ошибка
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    // 3. Данные загружены, но их нет (пустой результат)
    //    При этом фильтры остаются видимыми и доступными.
    const hasData = reportData.items.length > 0;

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

                {/* Выбор порядка сортировки.
                    Варианты «По замеров» удалены.
                    По умолчанию — «По дате выезда (новые сверху)» (exitDateDesc). */}
                <div className="flex items-center gap-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Сортировка:</label>
                    <select
                        value={sortOrder}
                        onChange={(e) => setSortOrder(e.target.value as PeriodSortOrder)}
                        className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                    >
                        <option value="exitDateDesc">По дате выезда (новые сверху)</option>
                        <option value="exitDateAsc">По дате выезда (старые сверху)</option>
                        <option value="averageHumidityAsc">По влажности (ниже сверху)</option>
                        <option value="averageHumidityDesc">По влажности (выше сверху)</option>
                        <option value="lastMeasurementDesc">По последнему замеру (новые сверху)</option>
                    </select>
                </div>

                <button
                    onClick={resetFilter}
                    className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                    title="Сбросить фильтр к последним 7 дням и сортировке по умолчанию"
                >
                    <RotateCcw className="w-4 h-4" />
                    Сбросить
                </button>
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

            {/* --- Отображение данных или сообщение об их отсутствии --- */}
            {!hasData ? (
                <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                    <LayoutGrid className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                    <p className="text-lg font-medium">Нет данных</p>
                    <p className="text-sm mt-1">За выбранный период замеры не найдены</p>
                    <button
                        onClick={resetFilter}
                        className="mt-4 inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition"
                    >
                        <RotateCcw className="w-4 h-4" />
                        Сбросить фильтр
                    </button>
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