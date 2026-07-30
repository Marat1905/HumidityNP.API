import { useState, useEffect } from 'react';
import { subDays } from 'date-fns';
import { useTopSuppliers } from '../hooks/useTopSuppliers';
import RangeDatePicker from '../components/shared/RangeDatePicker';
import { SkeletonTable } from '../components/shared/Skeleton';
import TopSuppliersChart from '../components/vehicles/TopSuppliersChart';
import { TrendingUp, TrendingDown, Droplet, Truck, Activity, BarChart, Table } from 'lucide-react';

export default function TopSuppliersPage() {
    const DEFAULT_DAYS = 30;

    // Инициализация дат
    const [startDate, setStartDate] = useState<Date>(() => {
        const now = new Date();
        return subDays(now, DEFAULT_DAYS);
    });
    const [endDate, setEndDate] = useState<Date>(() => new Date());

    const [topCount, setTopCount] = useState<number>(10);
    // Переключатель вида для каждой категории (график или таблица)
    const [viewModeGood, setViewModeGood] = useState<'chart' | 'table'>('chart');
    const [viewModeBad, setViewModeBad] = useState<'chart' | 'table'>('chart');

    // Получаем хороших (asc) и плохих (desc)
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

    // Объединённый рефреш
    const handleRefresh = () => {
        refetchGood();
        refetchBad();
    };

    // При изменении дат или количества перезапрашиваем
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

    const isLoading = loadingGood || loadingBad;
    const hasError = errorGood || errorBad;

    if (isLoading) {
        return (
            <div>
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Топ поставщиков по влажности</h2>
                <SkeletonTable rows={5} columns={5} />
            </div>
        );
    }

    if (hasError) {
        return (
            <div className="text-red-500 text-center py-10">
                {(errorGood || errorBad)?.message || 'Ошибка загрузки данных'}
            </div>
        );
    }

    // Вспомогательный компонент для рендеринга таблицы (вынесен из функции для читаемости)
    const renderTable = (suppliers: SupplierDto[], rankType: 'good' | 'bad') => {
        const getRowColor = (index: number) => {
            if (rankType === 'good') {
                if (index === 0) return 'bg-green-50 dark:bg-green-900/20 border-green-300 dark:border-green-700';
                if (index === 1) return 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700';
                if (index === 2) return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-300 dark:border-yellow-700';
            } else {
                if (index === 0) return 'bg-red-50 dark:bg-red-900/20 border-red-300 dark:border-red-700';
                if (index === 1) return 'bg-orange-50 dark:bg-orange-900/20 border-orange-300 dark:border-orange-700';
                if (index === 2) return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-300 dark:border-yellow-700';
            }
            return '';
        };

        return (
            <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                №
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Поставщик
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Средняя влажность
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Замеров
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Машин
                            </th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {suppliers.map((supplier, index) => (
                            <tr
                                key={supplier.inn}
                                className={`hover:bg-gray-50 dark:hover:bg-gray-800 transition ${getRowColor(index)}`}
                            >
                                <td className="px-4 py-3 text-sm font-bold text-gray-700 dark:text-gray-300">
                                    {index + 1}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                    <div>
                                        <div className="font-medium">{supplier.counterparty}</div>
                                        <div className="text-xs text-gray-500 dark:text-gray-400">ИНН: {supplier.inn}</div>
                                    </div>
                                </td>
                                <td className="px-4 py-3 text-sm font-semibold">
                                    <span className={rankType === 'good' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
                                        {supplier.averageHumidity !== null ? supplier.averageHumidity.toFixed(1) + '%' : '—'}
                                    </span>
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {supplier.totalMeasurements}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {supplier.vehiclesCount}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        );
    };

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
                            <option value={5}>5</option>
                            <option value={10}>10</option>
                            <option value={20}>20</option>
                            <option value={50}>50</option>
                        </select>
                    </div>
                </div>
            </div>

            {/* Блоки с хорошими и плохими поставщиками */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Хорошие (низкая влажность) */}
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
                    {goodSuppliers.length === 0 ? (
                        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                            Нет данных за выбранный период
                        </div>
                    ) : (
                        <>
                            {viewModeGood === 'chart' && (
                                <TopSuppliersChart suppliers={goodSuppliers} rankType="good" maxItems={topCount} />
                            )}
                            {viewModeGood === 'table' && renderTable(goodSuppliers, 'good')}
                        </>
                    )}
                </div>

                {/* Плохие (высокая влажность) */}
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
                    {badSuppliers.length === 0 ? (
                        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                            Нет данных за выбранный период
                        </div>
                    ) : (
                        <>
                            {viewModeBad === 'chart' && (
                                <TopSuppliersChart suppliers={badSuppliers} rankType="bad" maxItems={topCount} />
                            )}
                            {viewModeBad === 'table' && renderTable(badSuppliers, 'bad')}
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}