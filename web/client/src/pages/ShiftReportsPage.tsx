// src/pages/ShiftReportsPage.tsx
import { useState, useEffect } from 'react';
import { useShiftReport, type ShiftType } from '../hooks/useShiftReport';
import ShiftReportTable from '../components/ShiftReportTable';
import Spinner from '../components/Spinner';
import { ChevronLeft, ChevronRight, Calendar } from 'lucide-react';
import { format, subDays, addDays, startOfDay } from 'date-fns';
import { ru } from 'date-fns/locale';

export default function ShiftReportsPage() {
    const [selectedDate, setSelectedDate] = useState<Date>(() => {
        const now = new Date();
        return startOfDay(now);
    });

    const [shiftType, setShiftType] = useState<ShiftType>(() => {
        const hour = new Date().getHours();
        return (hour >= 8 && hour < 20) ? 'day' : 'night';
    });

    const { data, loading, error, refetch } = useShiftReport(selectedDate, shiftType);

    useEffect(() => {
        refetch();
    }, [selectedDate, shiftType, refetch]);

    const goToPrevDay = () => {
        setSelectedDate(prev => subDays(prev, 1));
    };

    const goToNextDay = () => {
        const tomorrow = addDays(selectedDate, 1);
        if (tomorrow <= startOfDay(new Date())) {
            setSelectedDate(tomorrow);
        }
    };

    const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newDate = new Date(e.target.value);
        if (!isNaN(newDate.getTime())) {
            setSelectedDate(startOfDay(newDate));
        }
    };

    const canGoNext = (() => {
        const tomorrow = addDays(selectedDate, 1);
        return tomorrow <= startOfDay(new Date());
    })();

    const dateDisplay = format(selectedDate, 'dd MMMM yyyy', { locale: ru });
    const shiftLabel = shiftType === 'day' ? 'Дневная (08:00–20:00)' : 'Ночная (20:00–08:00)';

    return (
        <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Отчёты по сменам
            </h2>

            <div className="flex flex-wrap items-center gap-4 mb-6 p-4 bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2">
                    <button
                        onClick={goToPrevDay}
                        className="p-2 rounded-lg border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition"
                        aria-label="Предыдущий день"
                    >
                        <ChevronLeft className="w-5 h-5" />
                    </button>
                    <button
                        onClick={goToNextDay}
                        disabled={!canGoNext}
                        className={`p-2 rounded-lg border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition disabled:opacity-50 disabled:cursor-not-allowed`}
                        aria-label="Следующий день"
                    >
                        <ChevronRight className="w-5 h-5" />
                    </button>
                </div>

                <div className="flex items-center gap-2">
                    <Calendar className="w-5 h-5 text-gray-500 dark:text-gray-400" />
                    <input
                        type="date"
                        value={format(selectedDate, 'yyyy-MM-dd')}
                        onChange={handleDateChange}
                        max={format(new Date(), 'yyyy-MM-dd')}
                        className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                    />
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        {dateDisplay}
                    </span>
                </div>

                <div className="flex items-center gap-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Смена:</label>
                    <select
                        value={shiftType}
                        onChange={(e) => setShiftType(e.target.value as ShiftType)}
                        className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
                    >
                        <option value="day">День (08:00–20:00)</option>
                        <option value="night">Ночь (20:00–08:00)</option>
                    </select>
                </div>

                <div className="ml-auto text-sm text-gray-500 dark:text-gray-400">
                    {shiftLabel}
                </div>
            </div>

            {loading ? (
                <Spinner />
            ) : error ? (
                <div className="text-red-500 text-center py-10">{error}</div>
            ) : data ? (
                <>
                    <div className="mb-2 text-sm text-gray-600 dark:text-gray-300">
                        Период: {format(data.shiftStart, 'dd MMM yyyy HH:mm', { locale: ru })} – {format(data.shiftEnd, 'dd MMM yyyy HH:mm', { locale: ru })}
                    </div>
                    <ShiftReportTable items={data.items} summary={data.summary} />
                </>
            ) : null}
        </div>
    );
}