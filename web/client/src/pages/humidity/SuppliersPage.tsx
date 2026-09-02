import React, { useState } from 'react';
import { subDays } from 'date-fns';
import { useSuppliers } from '../../hooks/humidity/useSuppliers';
import SupplierList from '../../components/humidity/SupplierList';
import Pagination from '../../components/common/Pagination';
import RangeDatePicker from '../../components/common/RangeDatePicker';
import { SkeletonTable } from '../../components/common/Skeleton';

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

    const toggleSupplier = (inn: string) => {
        setExpandedInn(prev => (prev === inn ? null : inn));
    };

    if (loading) return <SkeletonTable rows={5} columns={4} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

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
                </div>
            </div>

            {data && data.items.length === 0 ? (
                <div className="text-center py-10 text-gray-500 dark:text-gray-400">
                    Нет поставщиков за выбранный период
                </div>
            ) : (
                <>
                    <SupplierList
                        suppliers={data?.items || []}
                        expandedInn={expandedInn}
                        onToggle={toggleSupplier}
                        fromDate={startDate}
                        toDate={endDate}
                    />

                    {data && (
                        <Pagination
                            currentPage={data.pageNumber}
                            totalPages={data.totalPages}
                            onPageChange={setPageNumber}
                            pageSize={data.pageSize}
                            onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                            totalCount={data.totalCount}
                        />
                    )}
                </>
            )}
        </div>
    );
}