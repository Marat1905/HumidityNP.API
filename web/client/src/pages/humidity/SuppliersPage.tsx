import React, { useState, useEffect } from 'react';
import { subDays } from 'date-fns';
import { useSuppliers } from '../../hooks/humidity';
import { SupplierList } from '../../components/humidity';
import { Pagination, RangeDatePicker, SkeletonTable } from '../../components/common';
import { RotateCcw, Search, X } from 'lucide-react';

/**
 * Хук-дебаунс: возвращает значение `value` с задержкой `delay` мс.
 * Используется, чтобы не дёргать API на каждое нажатие клавиши в поле поиска.
 */
const useDebounce = <T,>(value: T, delay: number): T => {
    const [debouncedValue, setDebouncedValue] = useState<T>(value);

    useEffect(() => {
        const handler = setTimeout(() => setDebouncedValue(value), delay);
        return () => clearTimeout(handler);
    }, [value, delay]);

    return debouncedValue;
};

export default function SuppliersPage() {
    const DEFAULT_DAYS = 30;

    // Инициализируем даты сразу, чтобы они никогда не были null
    const [startDate, setStartDate] = useState<Date>(() => {
        const now = new Date();
        return subDays(now, DEFAULT_DAYS);
    });

    const [endDate, setEndDate] = useState<Date>(() => new Date());
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [expandedInn, setExpandedInn] = useState<string | null>(null);

    // Состояние поиска. `searchInput` обновляется на каждое нажатие клавиши,
    // `debouncedSearch` — с задержкой 400 мс. Именно debounced-значение уходит в API.
    const [searchInput, setSearchInput] = useState('');
    const debouncedSearch = useDebounce(searchInput, 400);

    // При смене значения поиска (debounced) сбрасываем пагинацию на первую страницу.
    // Это важно: иначе можно остаться на «странице 5», которой уже нет в новой выборке.
    useEffect(() => {
        setPageNumber(1);
        setExpandedInn(null);
    }, [debouncedSearch]);

    const { data, loading, error, refetch } = useSuppliers(
        startDate,
        endDate,
        pageNumber,
        pageSize,
        debouncedSearch.trim() || undefined
    );

    const handleDateRangeChange = (dates: [Date | null, Date | null]) => {
        const [start, end] = dates;
        // Если пользователь очистил диапазон – устанавливаем значения по умолчанию
        if (!start || !end) {
            const now = new Date();
            setStartDate(subDays(now, DEFAULT_DAYS));
            setEndDate(now);
        } else {
            setStartDate(start);
            setEndDate(end);
        }
        setPageNumber(1);
        setExpandedInn(null);
    };

    // Сброс фильтра – возвращаем к диапазону по умолчанию (последние 30 дней)
    // и очищаем поисковую строку.
    const resetFilter = () => {
        const now = new Date();
        setStartDate(subDays(now, DEFAULT_DAYS));
        setEndDate(now);
        setSearchInput('');
        setPageNumber(1);
        setExpandedInn(null);
    };

    const toggleSupplier = (inn: string) => {
        setExpandedInn(prev => (prev === inn ? null : inn));
    };

    // Есть ли активные фильтры (для показа кнопки «Сбросить все» и логики пустого состояния).
    const hasActiveFilters = !!searchInput.trim();

    // --- Обработка состояний ---
    // 1. Загрузка (только при первой загрузке без данных — иначе показываем скелетон
    //    на месте таблицы, чтобы не дёргать весь экран при смене страницы пагинации)
    if (loading && !data) return <SkeletonTable rows={5} columns={4} />;

    // 2. Ошибка
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    // 3. Данные загружены, но их нет (пустой результат).
    //    При этом фильтры остаются видимыми и доступными.
    const items = data?.items ?? [];
    const hasData = items.length > 0;

    return (
        <div>
            {/* Заголовок вкладки — отдельно, как в ReportPeriodPage */}
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Поставщики
            </h2>

            {/* Панель фильтров — в стиле отчёта за период:
                отдельная карточка с рамкой, фоном, скруглением и лёгкой тенью.
                Внутри размещены поиск, период и кнопка сброса. */}
            <div className="flex flex-wrap items-center gap-4 mb-6 p-4 bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700">
                {/* Поле поиска по ИНН или наименованию поставщика.
                    Ширина w-80 (320px) — длинные названия и ИНН помещаются целиком. */}
                <div className="relative w-80">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Search className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                    </div>
                    <input
                        type="text"
                        value={searchInput}
                        onChange={(e) => setSearchInput(e.target.value)}
                        placeholder="Поиск по названию или ИНН"
                        className="w-full pl-9 pr-8 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                    />
                    {searchInput && (
                        <button
                            onClick={() => setSearchInput('')}
                            className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                            aria-label="Очистить поиск"
                        >
                            <X className="w-4 h-4" />
                        </button>
                    )}
                </div>

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

                <button
                    onClick={resetFilter}
                    className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                    title="Сбросить фильтр к последним 30 дням и очистить поиск"
                >
                    <RotateCcw className="w-4 h-4" />
                    Сбросить
                </button>
            </div>

            {/* --- Отображение данных или сообщение об их отсутствии --- */}
            {!hasData ? (
                <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                    <RotateCcw className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                    <p className="text-lg font-medium">Нет поставщиков</p>
                    <p className="text-sm mt-1">
                        {hasActiveFilters
                            ? 'По вашему запросу ничего не найдено. Попробуйте изменить условия поиска.'
                            : 'За выбранный период поставщики не найдены'}
                    </p>
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
                    <SupplierList
                        suppliers={items}
                        expandedInn={expandedInn}
                        onToggle={toggleSupplier}
                        fromDate={startDate}
                        toDate={endDate}
                    />
                    {data && (
                        <Pagination
                            currentPage={data.pageNumber ?? 1}
                            totalPages={data.totalPages ?? 1}
                            onPageChange={setPageNumber}
                            pageSize={data.pageSize ?? 10}
                            onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                            totalCount={data.totalCount ?? 0}
                        />
                    )}
                </>
            )}
        </div>
    );
}