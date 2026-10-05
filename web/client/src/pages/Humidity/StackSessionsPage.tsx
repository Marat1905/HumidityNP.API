import { useState, useMemo } from 'react';
import { format, subDays } from 'date-fns';
import { useStackSessions } from '../../hooks/humidity';
import { RangeDatePicker, SkeletonTable } from '../../components/common';
import { StackGroup, StackSessionsSummary, StackSessionsTable } from '../../components/humidity';
import { Package, RotateCcw, RefreshCw, LayoutGrid, Table as TableIcon } from 'lucide-react';
import type { StackSessionDto } from '../../types/humidity';

type ViewMode = 'groups' | 'table';

/**
 * Страница «По штабелям».
 *
 * Два режима отображения:
 *  - «Группы» (по умолчанию) — аккордеон по штабелям, внутри — карточки сессий;
 *  - «Таблица» — плотный вид «всё сразу» на одном экране.
 *
 * Сверху — сводная статистика (штабелей, сессий, замеров, средняя влажность,
 * средняя длительность) и пояснение расхождения с «Отчётом за период»:
 * в отчёт по штабелям не попадают замеры машин с пустым StackNumber.
 * Плитки считаются на клиенте из уже загруженных сессий, а числа
 * «всего замеров» и «без штабеля» приходят с сервера в stats.
 *
 * Фильтры: только период. Вся агрегация — на сервере.
 */
export default function StackSessionsPage() {
    const DEFAULT_DAYS = 30;

    const [startDate, setStartDate] = useState<Date>(() => subDays(new Date(), DEFAULT_DAYS));
    const [endDate, setEndDate] = useState<Date>(() => new Date());
    const [viewMode, setViewMode] = useState<ViewMode>('groups');

    // Хук возвращает и сами сессии (data), и сводную статистику (stats),
    // которая нужна для пояснения расхождения с «Отчётом за период».
    const { data, stats, loading, error, refetch } = useStackSessions(startDate, endDate);

    /**
     * Группируем сессии по номеру штабеля.
     * Порядок штабелей — по «числовой» части номера (если она есть),
     * чтобы «Штабель 2» шёл раньше «Штабель 10».
     */
    const groupedSessions = useMemo(() => {
        const map = new Map<string, StackSessionDto[]>();
        for (const s of data) {
            if (!map.has(s.stackNumber)) map.set(s.stackNumber, []);
            map.get(s.stackNumber)!.push(s);
        }

        const entries = Array.from(map.entries());
        entries.sort((a, b) => {
            const numA = parseInt(a[0].replace(/\D/g, ''), 10);
            const numB = parseInt(b[0].replace(/\D/g, ''), 10);
            const hasNumA = !isNaN(numA);
            const hasNumB = !isNaN(numB);
            if (hasNumA && hasNumB) return numA - numB;
            if (hasNumA) return -1;
            if (hasNumB) return 1;
            return a[0].localeCompare(b[0]);
        });

        return entries;
    }, [data]);

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

    const periodLabel = `с ${format(startDate, 'dd.MM.yyyy')} по ${format(endDate, 'dd.MM.yyyy')}`;

    // --- Состояния ---
    if (loading && data.length === 0) return <SkeletonTable rows={5} columns={8} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    const hasData = data.length > 0;

    return (
        <div>
            {/* Заголовок с переключателем вида */}
            <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
                    По штабелям
                    <span className="ml-2 text-sm font-normal text-gray-500 dark:text-gray-400">
                        ({groupedSessions.length} {pluralizeStacks(groupedSessions.length)})
                    </span>
                </h2>

                <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-500 dark:text-gray-400 mr-1">Вид:</span>
                    <button
                        onClick={() => setViewMode('groups')}
                        className={`p-2 rounded-lg border transition ${viewMode === 'groups'
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                            : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'}`}
                        title="Группы по штабелям"
                    >
                        <LayoutGrid className="w-5 h-5" />
                    </button>
                    <button
                        onClick={() => setViewMode('table')}
                        className={`p-2 rounded-lg border transition ${viewMode === 'table'
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                            : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'}`}
                        title="Таблица"
                    >
                        <TableIcon className="w-5 h-5" />
                    </button>
                </div>
            </div>

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
                <div className="text-sm text-gray-500 dark:text-gray-400">{periodLabel}</div>
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
                    disabled={loading}
                    className="ml-auto inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition disabled:opacity-50"
                >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                    Обновить
                </button>
            </div>

            {!hasData ? (
                <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                    <Package className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                    <p className="text-lg font-medium">Нет сессий</p>
                    <p className="text-sm mt-1">За выбранный период сессии по штабелям не найдены</p>
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
                    {/* Сводка по всем сессиям за период + пояснение расхождения
                        с «Отчётом за период» (stats приходит с сервера). */}
                    <StackSessionsSummary sessions={data} stats={stats} />

                    {/* Содержимое в зависимости от выбранного вида */}
                    {viewMode === 'groups' ? (
                        <div className="space-y-3">
                            {groupedSessions.map(([stackNumber, sessions], idx) => (
                                <StackGroup
                                    key={stackNumber}
                                    stackNumber={stackNumber}
                                    sessions={sessions}
                                    // Первую группу раскрываем сразу — чтобы пользователь
                                    // не видел «пустой» экран из одних заголовков.
                                    defaultExpanded={idx === 0}
                                />
                            ))}
                        </div>
                    ) : (
                        <StackSessionsTable sessions={data} />
                    )}
                </>
            )}
        </div>
    );
}

/**
 * Склонение слова «штабель» для русского языка.
 */
function pluralizeStacks(n: number): string {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) return 'штабель';
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return 'штабеля';
    return 'штабелей';
}