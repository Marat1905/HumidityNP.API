import { useState } from 'react';
import { format, subDays } from 'date-fns';
import { ru } from 'date-fns/locale';
import { useStackSessions } from '../../hooks/humidity';
import { RangeDatePicker, SkeletonTable } from '../../components/common';
import { Package, RotateCcw, Truck, Users, Droplet, Clock, TrendingDown, TrendingUp } from 'lucide-react';

/**
 * Страница «По штабелям».
 *
 * Показывает сессии работы со штабелями за период:
 *  - сессии формируются на сервере SQL-запросом с оконными функциями;
 *  - базовый разрыв между замерами — 24 часа;
 *  - короткие осколки склеиваются с предыдущей сессией, если разрыв < 72 часов;
 *  - поставщики группируются по ИНН, при его отсутствии — по нормализованному имени;
 *  - у каждой сессии показывается основной поставщик и полный список.
 *
 * Влажности в DTO — nullable: null означает «нет данных» и показывается как «—»,
 * а не как «0.00%» (иначе на клиенте значение уходило бы в красную зону).
 *
 * Фильтры: только период. Вся агрегация — на сервере.
 */
export default function StackSessionsPage() {
    const DEFAULT_DAYS = 30;

    const [startDate, setStartDate] = useState<Date>(() => {
        const now = new Date();
        return subDays(now, DEFAULT_DAYS);
    });
    const [endDate, setEndDate] = useState<Date>(() => new Date());

    const { data, loading, error, refetch } = useStackSessions(startDate, endDate);

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

    const resetFilter = () => {
        const now = new Date();
        setStartDate(subDays(now, DEFAULT_DAYS));
        setEndDate(now);
    };

    const periodLabel = (() => {
        const fromStr = format(startDate, 'dd.MM.yyyy');
        const toStr = format(endDate, 'dd.MM.yyyy');
        return `с ${fromStr} по ${toStr}`;
    })();

    // --- Состояния ---
    if (loading && data.length === 0) return <SkeletonTable rows={5} columns={8} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    const hasData = data.length > 0;

    // Форматирование даты сессии (локальное время Екатеринбурга, как отдаёт сервер).
    const formatSessionDate = (iso: string) => {
        // Строка без таймзоны — парсим как локальную, отображаем как есть.
        return format(new Date(iso), 'dd.MM.yyyy HH:mm', { locale: ru });
    };

    return (
        <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                По штабелям
            </h2>

            {/* Панель фильтров */}
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
                <button
                    onClick={resetFilter}
                    className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                    title="Сбросить фильтр к последним 30 дням"
                >
                    <RotateCcw className="w-4 h-4" />
                    Сбросить
                </button>
                <button
                    onClick={() => refetch()}
                    className="ml-auto inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition"
                >
                    Обновить
                </button>
            </div>

            {/* Отображение данных */}
            {!hasData ? (
                <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                    <Package className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                    <p className="text-lg font-medium">Нет сессий</p>
                    <p className="text-sm mt-1">За выбранный период сессии по штабелям не найдены</p>
                </div>
            ) : (
                <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm">
                    <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                        <thead className="bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-700">
                            <tr>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Штабель
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Сессия
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Начало
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Конец
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1">
                                        <Clock className="w-3 h-3" />
                                        Длительность
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Замеров
                                </th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1 justify-center">
                                        <Truck className="w-3 h-3" />
                                        Машин
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1 justify-center">
                                        <Users className="w-3 h-3" />
                                        Поставщиков
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Основной поставщик
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Список поставщиков
                                </th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1 justify-center">
                                        <Droplet className="w-3 h-3" />
                                        Ср.
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Мин / Макс
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                            {data.map((s) => (
                                <tr
                                    key={`${s.stackNumber}-${s.sessionId}`}
                                    className="hover:bg-gray-50 dark:hover:bg-gray-800 transition"
                                >
                                    <td className="px-4 py-3 text-sm font-semibold text-gray-900 dark:text-white whitespace-nowrap">
                                        {s.stackNumber}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                        #{s.sessionId}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                        {formatSessionDate(s.sessionStart)}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                        {formatSessionDate(s.sessionEnd)}
                                    </td>
                                    <td className="px-4 py-3 text-sm font-mono text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                        {s.duration}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-center text-gray-700 dark:text-gray-300">
                                        {s.measurementsCount}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-center text-gray-700 dark:text-gray-300">
                                        {s.vehiclesCount}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-center text-gray-700 dark:text-gray-300">
                                        {s.counterpartyCount}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-white font-medium">
                                        {s.primaryCounterparty || '—'}
                                    </td>
                                    <td className="px-4 py-3 text-xs text-gray-600 dark:text-gray-400 max-w-md">
                                        {s.counterparties || '—'}
                                    </td>
                                    {/* Влажности nullable: null → «—», а не «0.00%» */}
                                    <td className="px-4 py-3 text-sm text-center font-semibold text-gray-900 dark:text-white whitespace-nowrap">
                                        {s.averageHumidity !== null
                                            ? s.averageHumidity.toFixed(2) + '%'
                                            : '—'}
                                    </td>
                                    <td className="px-4 py-3 text-sm text-center whitespace-nowrap">
                                        {s.minHumidity !== null && s.maxHumidity !== null ? (
                                            <>
                                                <span className="inline-flex items-center gap-1 text-blue-600 dark:text-blue-400">
                                                    <TrendingDown className="w-3.5 h-3.5" />
                                                    {s.minHumidity.toFixed(2)}%
                                                </span>
                                                <span className="text-gray-400 mx-1">/</span>
                                                <span className="inline-flex items-center gap-1 text-red-600 dark:text-red-400">
                                                    <TrendingUp className="w-3.5 h-3.5" />
                                                    {s.maxHumidity.toFixed(2)}%
                                                </span>
                                            </>
                                        ) : (
                                            <span className="text-gray-400 dark:text-gray-500">—</span>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}