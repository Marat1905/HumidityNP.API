import React, { useState, useRef, useLayoutEffect } from "react";
import { FiCalendar } from "react-icons/fi";
import { format, subDays, subMonths, startOfMonth, endOfMonth } from "date-fns";
import { ru } from "date-fns/locale";
import { DateRange } from 'react-date-range';
import type { Range, RangeKeyDict } from 'react-date-range';
import 'react-date-range/dist/styles.css';
import 'react-date-range/dist/theme/default.css';

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

    const buttonRef = useRef<HTMLButtonElement>(null);
    const dropdownRef = useRef<HTMLDivElement>(null);
    // Инициализируем позицию по умолчанию (левая привязка)
    const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({ left: 0 });

    // Синхронное вычисление позиции при открытии
    useLayoutEffect(() => {
        if (!isOpen) {
            // При закрытии сбрасываем стили, но оставляем left: 0 для плавности
            setDropdownStyle({ left: 0 });
            return;
        }

        // Даём браузеру минимальное время для рендера, но используем requestAnimationFrame только для измерения,
        // а не для изменения стилей – изменения делаем сразу после получения размеров.
        // Для этого используем getBoundingClientRect синхронно.
        const buttonEl = buttonRef.current;
        const dropdownEl = dropdownRef.current;
        if (!buttonEl || !dropdownEl) return;

        const buttonRect = buttonEl.getBoundingClientRect();
        const dropdownWidth = dropdownEl.offsetWidth || 600; // примерная ширина, если ещё не измерена
        const viewportWidth = window.innerWidth;

        const spaceLeft = buttonRect.left;
        const spaceRight = viewportWidth - buttonRect.right;

        let newStyle: React.CSSProperties = {};

        // Если справа достаточно места (и больше, чем слева), выпадаем вправо
        if (spaceRight > spaceLeft && spaceRight > dropdownWidth) {
            newStyle = { left: 0 };
        }
        // Иначе если слева достаточно места, выпадаем влево
        else if (spaceLeft > dropdownWidth) {
            newStyle = { right: 0 };
        }
        // Иначе центрируем (но это может вызвать мерцание, поэтому лучше прижать к левому краю, если места мало)
        else {
            // Если не хватает места ни слева, ни справа, прижимаем к левому краю экрана
            // с учётом небольшого отступа
            const leftOffset = Math.max(0, buttonRect.left - 20);
            newStyle = { left: leftOffset };
        }

        setDropdownStyle(newStyle);
    }, [isOpen]);

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
        return `${format(startDate, "dd.MM.yyyy")} - ${format(endDate, "dd.MM.yyyy")}`;
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
                const today = new Date();
                return [today, today] as [Date, Date];
            }
        },
        {
            label: "Вчера",
            icon: "📅",
            getDates: () => {
                const yesterday = subDays(new Date(), 1);
                return [yesterday, yesterday] as [Date, Date];
            }
        },
        {
            label: "Последние 7 дней",
            icon: "📆",
            getDates: () => {
                const end = new Date();
                const start = subDays(end, 6);
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Последние 30 дней",
            icon: "📊",
            getDates: () => {
                const end = new Date();
                const start = subDays(end, 29);
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Текущий месяц",
            icon: "🗓️",
            getDates: () => {
                const start = startOfMonth(new Date());
                const end = endOfMonth(new Date());
                return [start, end] as [Date, Date];
            }
        },
        {
            label: "Прошлый месяц",
            icon: "⏮️",
            getDates: () => {
                const date = new Date();
                const start = startOfMonth(subMonths(date, 1));
                const end = endOfMonth(subMonths(date, 1));
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
        <div className="relative w-full">
            <button
                ref={buttonRef}
                type="button"
                className={`border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 flex items-center justify-between w-full ${getButtonPaddingClasses()}`}
                onClick={() => setIsOpen(!isOpen)}
            >
                <span>{formatRange()}</span>
                <FiCalendar className="ml-2 text-gray-500 dark:text-gray-400" />
            </button>

            {isOpen && (
                <>
                    <div className="fixed inset-0 z-40" onClick={() => setIsOpen(false)} />
                    <div
                        ref={dropdownRef}
                        className="absolute z-50 mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-w-[95vw] w-auto min-w-[300px]"
                        style={dropdownStyle}
                    >
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
                                    <>Выбрано: <span className="font-semibold text-gray-900 dark:text-white">{format(range.startDate, "dd.MM.yyyy")} - {format(range.endDate, "dd.MM.yyyy")}</span></>
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