import { useState, useEffect } from 'react';
import { subDays } from 'date-fns';
import { useTopSuppliers } from '../../hooks/humidity/useTopSuppliers';
import { RangeDatePicker, SkeletonTable } from '../../components/common';
import { TopSuppliersChart, SuppliersTable } from '../../components/humidity'
import type { SupplierDto } from '../../types/humidity';
import { TrendingUp, TrendingDown, BarChart, Table, RotateCcw } from 'lucide-react';

export default function TopSuppliersPage() {
    const DEFAULT_DAYS = 30;

    const [startDate, setStartDate] = useState<Date>(() => {
        const now = new Date();
        return subDays(now, DEFAULT_DAYS);
    });
    const [endDate, setEndDate] = useState<Date>(() => new Date());

    const [topCount, setTopCount] = useState<number>(10);
    const [viewModeGood, setViewModeGood] = useState<'chart' | 'table'>('chart');
    const [viewModeBad, setViewModeBad] = useState<'chart' | 'table'>('chart');

    const {
        data: goodSuppliers,
        loading: loadingGood,
        error: errorGood,
        refetch: refetchGood
    } = useTopSuppliers(startDate, endDate, topCount, 'asc');

    const {
        data: badSuppliers,
        loading: loadingBad,
        error: errorBad,
        refetch: refetchBad
    } = useTopSuppliers(startDate, endDate, topCount, 'desc');

    const handleRefresh = () => {
        refetchGood();
        refetchBad();
    };

    useEffect(() => {
        handleRefresh();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [startDate, endDate, topCount]);

    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        if (!start || !end) {
            const now = new Date();
            setStartDate(subDays(now, DEFAULT_DAYS));
            setEndDate(now);
        } else {
            setStartDate(start);
            setEndDate(end);
        }
    };

    // Сброс фильтров – возвращаем к значениям по умолчанию
    const resetFilters = () => {
        const now = new Date();
        setStartDate(subDays(now, DEFAULT_DAYS));
        setEndDate(now);
        setTopCount(10);
    };

    const isLoading = loadingGood || loadingBad;
    const hasError = errorGood || errorBad;

    // --- Обработка состояний ---
    // 1. Загрузка
    if (isLoading) {
        return (
            <div>
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Топ поставщиков по влажности</h2>
                <SkeletonTable rows={5} columns={5} />
            </div>
        );
    }

    // 2. Ошибка
    if (hasError) {
        return (
            <div className="text-red-500 text-center py-10">
                {(errorGood || errorBad)?.message || 'Ошибка загрузки данных'}
            </div>
        );
    }

    // 3. Данные загружены, но их нет (пустой результат)
    //    При этом фильтры остаются видимыми и доступными.
    const hasGoodData = Array.isArray(goodSuppliers) && goodSuppliers.length > 0;
    const hasBadData = Array.isArray(badSuppliers) && badSuppliers.length > 0;

    return (
        <div>
            <div className="flex flex-wrap items-center justify-between gap-4 mb-6">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Топ поставщиков по влажности</h2>
                <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                        <span className="text-sm text-gray-600 dark:text-gray-300">Период:</span>
                        <div className="w-64">
                            <RangeDatePicker
                                startDate={startDate}
                                endDate={endDate}
                                onChange={handleDateRangeChange}
                                size="md"
                            />
                        </div>
                    </div>
                    <div className="flex items-center gap-2">
                        <label className="text-sm text-gray-600 dark:text-gray-300">Топ:</label>
                        <select
                            value={topCount}
                            onChange={(e) => setTopCount(Number(e.target.value))}
                            className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                        >
                            <option value={3}>3</option>
                            <option value={5}>5</option>
                            <option value={10}>10</option>
                            <option value={20}>20</option>
                            <option value={50}>50</option>
                        </select>
                    </div>
                    <button
                        onClick={resetFilters}
                        className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                        title="Сбросить фильтры к последним 30 дням и топ-10"
                    >
                        <RotateCcw className="w-4 h-4" />
                        Сбросить
                    </button>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Хорошие поставщики */}
                <div>
                    <div className="flex items-center justify-between mb-3">
                        <div className="flex items-center gap-2">
                            <TrendingDown className="w-6 h-6 text-green-500" />
                            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Лучшие поставщики</h3>
                            <span className="text-sm text-gray-500 dark:text-gray-400">(низкая влажность)</span>
                        </div>
                        <div className="flex items-center gap-1">
                            <button
                                onClick={() => setViewModeGood('chart')}
                                className={`p-1.5 rounded-md transition ${viewModeGood === 'chart'
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                                    : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'
                                    }`}
                                title="График"
                            >
                                <BarChart className="w-5 h-5" />
                            </button>
                            <button
                                onClick={() => setViewModeGood('table')}
                                className={`p-1.5 rounded-md transition ${viewModeGood === 'table'
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                                    : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'
                                    }`}
                                title="Таблица"
                            >
                                <Table className="w-5 h-5" />
                            </button>
                        </div>
                    </div>
                    {!hasGoodData ? (
                        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                            <TrendingDown className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                            <p className="text-lg font-medium">Нет данных</p>
                            <p className="text-sm mt-1">За выбранный период данные не найдены</p>
                        </div>
                    ) : (
                        <>
                            {viewModeGood === 'chart' && (
                                <TopSuppliersChart suppliers={goodSuppliers} rankType="good" maxItems={topCount} />
                            )}
                            {viewModeGood === 'table' && (
                                <SuppliersTable suppliers={goodSuppliers} rankType="good" />
                            )}
                        </>
                    )}
                </div>

                {/* Плохие поставщики */}
                <div>
                    <div className="flex items-center justify-between mb-3">
                        <div className="flex items-center gap-2">
                            <TrendingUp className="w-6 h-6 text-red-500" />
                            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Худшие поставщики</h3>
                            <span className="text-sm text-gray-500 dark:text-gray-400">(высокая влажность)</span>
                        </div>
                        <div className="flex items-center gap-1">
                            <button
                                onClick={() => setViewModeBad('chart')}
                                className={`p-1.5 rounded-md transition ${viewModeBad === 'chart'
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                                    : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'
                                    }`}
                                title="График"
                            >
                                <BarChart className="w-5 h-5" />
                            </button>
                            <button
                                onClick={() => setViewModeBad('table')}
                                className={`p-1.5 rounded-md transition ${viewModeBad === 'table'
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                                    : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'
                                    }`}
                                title="Таблица"
                            >
                                <Table className="w-5 h-5" />
                            </button>
                        </div>
                    </div>
                    {!hasBadData ? (
                        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                            <TrendingUp className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                            <p className="text-lg font-medium">Нет данных</p>
                            <p className="text-sm mt-1">За выбранный период данные не найдены</p>
                        </div>
                    ) : (
                        <>
                            {viewModeBad === 'chart' && (
                                <TopSuppliersChart suppliers={badSuppliers} rankType="bad" maxItems={topCount} />
                            )}
                            {viewModeBad === 'table' && (
                                <SuppliersTable suppliers={badSuppliers} rankType="bad" />
                            )}
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}