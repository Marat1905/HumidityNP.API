import React, { useState } from "react";
import { FiCalendar } from "react-icons/fi";
import moment from "moment";
import { DateRange } from 'react-date-range';
import type { Range, RangeKeyDict } from 'react-date-range';
import 'react-date-range/dist/styles.css';
import 'react-date-range/dist/theme/default.css';
import { ru } from 'date-fns/locale';

interface RangeDatePickerProps {
    startDate: Date | null;
    endDate: Date | null;
    onChange: (dates: [Date | null, Date | null]) => void;
    /** Размер кнопки: 'sm' (py-1.5), 'md' (py-2, по умолчанию), 'lg' (py-2.5) */
    size?: 'sm' | 'md' | 'lg';
}

const RangeDatePicker: React.FC<RangeDatePickerProps> = ({ startDate, endDate, onChange, size = 'md' }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [range, setRange] = useState<Range>({
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        key: 'selection'
    });

    const handleApply = () => {
        onChange([range.startDate || null, range.endDate || null]);
        setIsOpen(false);
    };

    const handleCancel = () => {
        setRange({
            startDate: startDate || undefined,
            endDate: endDate || undefined,
            key: 'selection'
        });
        setIsOpen(false);
    };

    const formatRange = () => {
        if (!startDate || !endDate) {
            return "Выберите диапазон дат";
        }
        return `${moment(startDate).format("DD.MM.YYYY")} - ${moment(endDate).format("DD.MM.YYYY")}`;
    };

    // Определяем классы для кнопки в зависимости от размера
    const getButtonPaddingClasses = () => {
        switch (size) {
            case 'sm':
                return 'px-3 py-1.5 text-sm';
            case 'lg':
                return 'px-3 py-2.5 text-base';
            default:
                return 'px-3 py-2 text-sm';
        }
    };

    const quickRanges = [
        {
            label: "Сегодня",
            icon: "🕐",
            getDates: () => {
                const today = moment().toDate();
                return [today, today] as [Date, Date];
            }
        },
        {
            label: "Вчера",
            icon: "📅",
            getDates: () => {
                const yesterday = moment().subtract(1, 'day').toDate();
                return [yesterday, yesterday] as [Date, Date];
            }
        },
        {
            label: "Последние 7 дней",
            icon: "📆",
            getDates: () => {
                const end = moment().toDate();
                const start = moment().subtract(6, 'days').toDate();
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Последние 30 дней",
            icon: "📊",
            getDates: () => {
                const end = moment().toDate();
                const start = moment().subtract(29, 'days').toDate();
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Текущий месяц",
            icon: "🗓️",
            getDates: () => {
                const start = moment().startOf('month').toDate();
                const end = moment().endOf('month').toDate();
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Прошлый месяц",
            icon: "⏮️",
            getDates: () => {
                const start = moment().subtract(1, 'month').startOf('month').toDate();
                const end = moment().subtract(1, 'month').endOf('month').toDate();
                return [start, end] as [Date, Date];
            }
        }
    ];

    const handleQuickRange = (getDates: () => [Date, Date]) => {
        const [newStart, newEnd] = getDates();
        setRange({
            startDate: newStart,
            endDate: newEnd,
            key: 'selection'
        });
    };

    return (
        <div className="relative">
            <button
                type="button"
                className={`border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 flex items-center justify-between w-full ${getButtonPaddingClasses()}`}
                onClick={() => setIsOpen(!isOpen)}
            >
                <span>{formatRange()}</span>
                <FiCalendar className="ml-2 text-gray-500 dark:text-gray-400" />
            </button>

            {isOpen && (
                <>
                    <div
                        className="fixed inset-0 z-40"
                        onClick={() => setIsOpen(false)}
                    />
                    <div className="absolute z-50 mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-w-[95vw] w-full md:w-auto right-0">
                        <div className="flex flex-col md:flex-row max-h-[80vh] overflow-auto">
                            <div className="w-full md:w-48 border-b md:border-b-0 md:border-r border-gray-200 dark:border-gray-700 p-4 flex-shrink-0">
                                <div className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Быстрые диапазоны</div>
                                <div className="space-y-1">
                                    {quickRanges.map((quickRange, index) => (
                                        <button
                                            key={index}
                                            type="button"
                                            className="
                                                w-full text-left px-4 py-3.5 rounded-xl
                                                text-sm font-medium text-gray-700 dark:text-gray-300 
                                                hover:bg-gradient-to-r hover:from-blue-50 hover:to-blue-50/50 dark:hover:from-gray-700 dark:hover:to-gray-700/50
                                                transition-all duration-200 ease-out
                                                flex items-center space-x-3
                                                hover:shadow-lg hover:shadow-blue-100/30 dark:hover:shadow-blue-900/10
                                                border border-transparent hover:border-blue-200 dark:hover:border-blue-800
                                                group/range
                                            "
                                            onClick={() => handleQuickRange(quickRange.getDates)}
                                        >
                                            <span className="text-xl group-hover/range:scale-110 transition-transform">{quickRange.icon}</span>
                                            <span className="group-hover/range:text-blue-600 dark:group-hover/range:text-blue-400 transition-colors">{quickRange.label}</span>
                                        </button>
                                    ))}
                                </div>
                            </div>
                            <div className="flex-1 overflow-auto p-2">
                                <DateRange
                                    editableDateInputs={true}
                                    onChange={(item: RangeKeyDict) => setRange(item.selection)}
                                    moveRangeOnFirstSelection={false}
                                    ranges={[range]}
                                    locale={ru}
                                    dateDisplayFormat="dd.MM.yyyy"
                                    rangeColors={["#3b82f6"]}
                                    showDateDisplay={false}
                                    showPreview={true}
                                    maxDate={new Date()}
                                    months={2}
                                    direction="horizontal"
                                    className="date-range-picker"
                                />
                            </div>
                        </div>
                        <div className="flex justify-between items-center p-6 border-t border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-white dark:from-gray-800 dark:to-gray-900">
                            <div className="text-sm font-medium text-gray-600 dark:text-gray-400">
                                {range.startDate && range.endDate && (
                                    <>Выбрано: <span className="font-semibold text-gray-900 dark:text-white">{moment(range.startDate).format("DD.MM.YYYY")} - {moment(range.endDate).format("DD.MM.YYYY")}</span></>
                                )}
                            </div>
                            <div className="flex space-x-3">
                                <button
                                    type="button"
                                    onClick={handleCancel}
                                    className="
                                        px-6 py-3 rounded-xl
                                        font-medium text-gray-600 dark:text-gray-400 
                                        hover:text-gray-900 dark:hover:text-gray-200
                                        hover:bg-gradient-to-r hover:from-gray-100 hover:to-gray-100/50 dark:hover:from-gray-700 dark:hover:to-gray-700/50
                                        transition-all duration-200
                                    "
                                >
                                    Отмена
                                </button>
                                <button
                                    type="button"
                                    onClick={handleApply}
                                    className="
                                        px-6 py-3 rounded-xl
                                        bg-gradient-to-r from-blue-500 to-blue-600 
                                        text-white font-semibold
                                        hover:from-blue-600 hover:to-blue-700
                                        transition-all duration-200
                                        shadow-lg hover:shadow-xl hover:shadow-blue-500/30
                                        transform hover:-translate-y-0.5
                                    "
                                >
                                    Применить
                                </button>
                            </div>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
};

export default React.memo(RangeDatePicker);