import { useState, useEffect } from 'react';
import { useShiftReport, type ShiftType } from '../../hooks/humidity';
import { ShiftReportTable, ShiftReportCardView } from '../../components/humidity';
import { SkeletonReport, DatePicker } from '../../components/common';
import { ChevronLeft, ChevronRight, LayoutGrid, Table } from 'lucide-react';
import { format, subDays, addDays, startOfDay } from 'date-fns';
import { ru } from 'date-fns/locale';

type ViewMode = 'table' | 'cards';

/**
 * Вычисляет дату начала текущей смены и её тип.
 *
 * Логика:
 *  - 08:00 – 19:59 → дневная смена (08:00–20:00), дата = сегодня;
 *  - 20:00 – 23:59 → ночная смена (20:00–08:00), дата = сегодня;
 *  - 00:00 – 07:59 → ночная смена (20:00–08:00), НАЧАЛАСЬ ВЧЕРА → дата = вчера.
 *
 * Пример: сейчас 07:30 14.09.2026 — мы всё ещё в ночной смене,
 * которая началась 13.09.2026 в 20:00 и закончится 14.09.2026 в 08:00.
 * Значит, стартовая дата = 13.09.2026, тип смены = 'night'.
 *
 * @returns Кортеж [стартовая дата (начало дня), тип смены]
 */
const getInitialShiftState = (): [Date, ShiftType] => {
    const now = new Date();
    const hour = now.getHours();

    // Раннее утро (до 08:00) — всё ещё тянется ночная смена, начавшаяся ВЧЕРА.
    // Именно поэтому стартовая дата — вчерашний день.
    if (hour < 8) {
        return [startOfDay(subDays(now, 1)), 'night'];
    }

    // Дневная смена: 08:00 – 19:59 — сегодняшний день.
    if (hour < 20) {
        return [startOfDay(now), 'day'];
    }

    // Вечер/ночь: 20:00 – 23:59 — сегодняшний день, ночная смена.
    return [startOfDay(now), 'night'];
};

export default function ShiftReportsPage() {
    // Инициализируем состояние один раз, используя общую функцию.
    // Это гарантирует согласованность даты и типа смены
    // (например, при 07:30 будет 13.09 + 'night', а не 14.09 + 'night').
    const [selectedDate, setSelectedDate] = useState<Date>(() => {
        const [initialDate] = getInitialShiftState();
        return initialDate;
    });

    const [shiftType, setShiftType] = useState<ShiftType>(() => {
        const [, initialShift] = getInitialShiftState();
        return initialShift;
    });

    const [viewMode, setViewMode] = useState<ViewMode>('table');

    const { data, loading, error, refetch } = useShiftReport(selectedDate, shiftType);

    useEffect(() => {
        refetch();
    }, [selectedDate, shiftType, refetch]);

    /**
     * Переход на предыдущий день.
     * Просто уменьшаем дату на 1 день — тип смены сохраняется тем же,
     * чтобы оператор мог листать однотипные смены подряд.
     */
    const goToPrevDay = () => {
        setSelectedDate(prev => subDays(prev, 1));
    };

    /**
     * Переход на следующий день.
     * Ограничение: нельзя уйти в «будущее» — максимальная доступная дата
     * определяется текущей сменой.
     *
     * Если сейчас идёт ночная смена (например, 07:30 14.09 — смена 13.09 night),
     * то следующей доступной датой будет 14.09 (там уже будет дневная смена 08:00–20:00).
     *
     * Если сейчас идёт дневная смена (например, 10:00 14.09 — смена 14.09 day),
     * то следующей доступной датой будет 14.09 (ночная смена начнётся вечером).
     * Кнопка «вперёд» в этом случае заблокирована.
     */
    const goToNextDay = () => {
        const tomorrow = addDays(selectedDate, 1);

        // Максимально допустимая дата — дата начала текущей смены (см. getInitialShiftState).
        const [maxDate] = getInitialShiftState();

        if (tomorrow <= maxDate) {
            setSelectedDate(tomorrow);
        }
    };

    /**
     * Можно ли перейти на следующий день.
     * Следующий день доступен, только если он не превышает дату начала текущей смены.
     */
    const canGoNext = (() => {
        const tomorrow = addDays(selectedDate, 1);
        const [maxDate] = getInitialShiftState();
        return tomorrow <= maxDate;
    })();

    const dateDisplay = format(selectedDate, 'dd MMMM yyyy', { locale: ru });
    const shiftLabel = shiftType === 'day' ? 'Дневная (08:00–20:00)' : 'Ночная (20:00–08:00)';

    if (loading) return <SkeletonReport />;

    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;

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
                        <ChevronLeft className="w-5 h-5 text-gray-700 dark:text-gray-200" />
                    </button>
                    <button
                        onClick={goToNextDay}
                        disabled={!canGoNext}
                        className={`p-2 rounded-lg border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition disabled:opacity-50 disabled:cursor-not-allowed`}
                        aria-label="Следующий день"
                    >
                        <ChevronRight className="w-5 h-5 text-gray-700 dark:text-gray-200" />
                    </button>
                </div>

                <div className="flex items-center gap-2">
                    <div className="w-48">
                        <DatePicker
                            date={selectedDate}
                            onChange={(newDate) => setSelectedDate(startOfDay(newDate))}
                            maxDate={startOfDay(new Date())}
                            size="md"
                        />
                    </div>
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

                <div className="text-sm text-gray-500 dark:text-gray-400 ml-2">
                    {shiftLabel}
                </div>
            </div>

            {data ? (
                <>
                    <div className="mb-2 text-sm text-gray-600 dark:text-gray-300">
                        Период: {format(data.shiftStart, 'dd MMM yyyy HH:mm', { locale: ru })} – {format(data.shiftEnd, 'dd MMM yyyy HH:mm', { locale: ru })}
                    </div>

                    {viewMode === 'table' ? (
                        <ShiftReportTable items={data.items} summary={data.summary} />
                    ) : (
                        <ShiftReportCardView items={data.items} summary={data.summary} />
                    )}
                </>
            ) : null}
        </div>
    );
}