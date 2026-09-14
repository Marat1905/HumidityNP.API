// src/pages/ReportPeriodPage.tsx

import { useState } from 'react';
import { format, subDays } from 'date-fns';
import { ru } from 'date-fns/locale';
import { usePeriodReport } from '../../hooks/humidity';
import { SkeletonReport, RangeDatePicker, Pagination } from '../../components/common';
import { PeriodReportCardView, PeriodReportTable } from '../../components/humidity';
import type { PeriodReportSortBy } from '../../types/humidity';
import { LayoutGrid, Table, RotateCcw } from 'lucide-react';

type ViewMode = 'table' | 'cards';

/**
 * Порядок сортировки отчёта за период.
 * Значение по умолчанию — 'exitDateDesc' (по дате выезда машины, новые сверху).
 *
 * ВАЖНО: вся сортировка и пагинация выполняются на сервере.
 * Фронтенд только транслирует выбранный вариант в параметры sortBy + order
 * и передаёт в хук usePeriodReport.
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

/**
 * Маппинг UI-значения сортировки в пару (sortBy, order) для API.
 * Все значения соответствуют серверному контракту эндпоинта /measurements/period-report.
 */
const mapSortOrderToApi = (sortOrder: PeriodSortOrder): { sortBy: PeriodReportSortBy; order: 'asc' | 'desc' } => {
    switch (sortOrder) {
        case 'exitDateAsc':
            return { sortBy: 'exitDate', order: 'asc' };
        case 'averageHumidityAsc':
            return { sortBy: 'averageHumidity', order: 'asc' };
        case 'averageHumidityDesc':
            return { sortBy: 'averageHumidity', order: 'desc' };
        case 'lastMeasurementDesc':
            return { sortBy: 'lastMeasurement', order: 'desc' };
        case 'exitDateDesc':
        default:
            return { sortBy: 'exitDate', order: 'desc' };
    }
};

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

    // Пагинация. Выполняется на сервере, поэтому при смене страницы
    // хук usePeriodReport перезапрашивает данные с новыми параметрами.
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(100);

    // Маппим UI-значение сортировки в параметры API.
    const { sortBy, order } = mapSortOrderToApi(sortOrder);

    // Загружаем агрегированный отчёт с сервера.
    // Хук сам следит за изменениями параметров и перезапрашивает данные.
    const { data, loading, error, refetch } = usePeriodReport(
        startDate,
        endDate,
        sortBy,
        order,
        pageNumber,
        pageSize
    );

    // Обработчик изменения диапазона из RangeDatePicker.
    // Сбрасываем пагинацию на первую страницу.
    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        setStartDate(start);
        setEndDate(end);
        setPageNumber(1);
    };

    // Сброс фильтра – возвращаем к диапазону по умолчанию (последние 7 дней),
    // порядку сортировки по умолчанию и первой странице.
    const resetFilter = () => {
        const now = new Date();
        setStartDate(subDays(now, 6));
        setEndDate(now);
        setSortOrder(DEFAULT_SORT_ORDER);
        setPageNumber(1);
    };

    // Формирование строки с периодом для отображения
    const periodLabel = (() => {
        if (!startDate || !endDate) return 'не выбран';
        const fromStr = format(startDate, 'dd.MM.yyyy');
        const toStr = format(endDate, 'dd.MM.yyyy');
        return `с ${fromStr} по ${toStr}`;
    })();

    // --- Обработка состояний ---
    // 1. Загрузка
    if (loading && !data) return <SkeletonReport />;

    // 2. Ошибка
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    // 3. Данные загружены, но их нет (пустой результат)
    //    При этом фильтры остаются видимыми и доступными.
    const hasData = (data?.vehicles.items?.length ?? 0) > 0;

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
                    Вся сортировка выполняется на сервере — выбранный вариант
                    транслируется в параметры sortBy + order и уходит в API. */}
                <div className="flex items-center gap-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Сортировка:</label>
                    <select
                        value={sortOrder}
                        onChange={(e) => {
                            setSortOrder(e.target.value as PeriodSortOrder);
                            setPageNumber(1); // сбрасываем на первую страницу при смене сортировки
                        }}
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
                            items={data!.vehicles.items}
                            summary={data!.summary}
                            periodLabel={periodLabel}
                        />
                    ) : (
                        <PeriodReportCardView
                            items={data!.vehicles.items}
                            summary={data!.summary}
                            periodLabel={periodLabel}
                        />
                    )}

                    {/* Пагинация: смена страницы вызывает новый запрос к серверу. */}
                    <Pagination
                        currentPage={data!.vehicles.pageNumber}
                        totalPages={data!.vehicles.totalPages}
                        onPageChange={setPageNumber}
                        pageSize={data!.vehicles.pageSize}
                        onPageSizeChange={(size) => {
                            setPageSize(size);
                            setPageNumber(1);
                        }}
                        totalCount={data!.vehicles.totalCount}
                    />
                </>
            )}
        </div>
    );
}