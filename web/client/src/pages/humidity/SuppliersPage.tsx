import React, { useState } from 'react';
import { subDays } from 'date-fns';
import { useSuppliers } from '../../hooks/humidity/useSuppliers';
import { SupplierList } from '../../components/humidity'
import { Pagination, RangeDatePicker, SkeletonTable } from '../../components/common';
import { RotateCcw } from 'lucide-react';

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

    const { data, loading, error, refetch } = useSuppliers(startDate, endDate, pageNumber, pageSize);

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
        setExpandedInn(null); // сбрасываем раскрытие
    };

    // Сброс фильтра – возвращаем к диапазону по умолчанию (последние 30 дней)
    const resetFilter = () => {
        const now = new Date();
        setStartDate(subDays(now, DEFAULT_DAYS));
        setEndDate(now);
        setPageNumber(1);
        setExpandedInn(null);
    };

    const toggleSupplier = (inn: string) => {
        setExpandedInn(prev => (prev === inn ? null : inn));
    };

    // --- Обработка состояний ---
    // 1. Загрузка
    if (loading) return <SkeletonTable rows={5} columns={4} />;

    // 2. Ошибка
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

    // 3. Данные загружены, но их нет (пустой результат)
    //    При этом фильтры остаются видимыми и доступными.
    const items = data?.items ?? [];
    const hasData = items.length > 0;

    return (
        <div>
            <div className="flex flex-wrap items-center justify-between gap-4 mb-6">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Поставщики</h2>
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
                    <button
                        onClick={resetFilter}
                        className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1"
                        title="Сбросить фильтр к последним 30 дням"
                    >
                        <RotateCcw className="w-4 h-4" />
                        Сбросить
                    </button>
                </div>
            </div>

            {/* --- Отображение данных или сообщение об их отсутствии --- */}
            {!hasData ? (
                <div className="text-center py-12 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700">
                    <RotateCcw className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                    <p className="text-lg font-medium">Нет поставщиков</p>
                    <p className="text-sm mt-1">За выбранный период поставщики не найдены</p>
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