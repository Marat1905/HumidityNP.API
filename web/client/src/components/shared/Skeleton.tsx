// src/components/Skeleton.tsx
import React from 'react';

/**
 * Базовый скелетон с анимацией пульсации.
 */
const SkeletonBase: React.FC<{ className?: string }> = ({ className = '' }) => (
    <div className={`animate-pulse bg-gray-200 dark:bg-gray-700 rounded ${className}`} />
);

/**
 * Скелетон для таблицы.
 * @param rows - количество строк (заглушек)
 * @param columns - количество столбцов (заглушек)
 */
export const SkeletonTable: React.FC<{ rows?: number; columns?: number }> = ({
    rows = 5,
    columns = 6,
}) => {
    return (
        <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                    <tr>
                        {Array.from({ length: columns }).map((_, i) => (
                            <th key={i} className="px-4 py-3">
                                <SkeletonBase className="h-4 w-3/4" />
                            </th>
                        ))}
                    </tr>
                </thead>
                <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                    {Array.from({ length: rows }).map((_, rowIdx) => (
                        <tr key={rowIdx}>
                            {Array.from({ length: columns }).map((_, colIdx) => (
                                <td key={colIdx} className="px-4 py-3">
                                    <SkeletonBase className="h-4 w-full" />
                                </td>
                            ))}
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
};

/**
 * Скелетон для страниц отчётов (сменный и за период).
 * Содержит заглушки для блоков статистики и таблицы.
 */
export const SkeletonReport: React.FC = () => {
    return (
        <div>
            {/* Блоки статистики */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                {Array.from({ length: 4 }).map((_, i) => (
                    <div
                        key={i}
                        className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-200 dark:border-gray-700"
                    >
                        <div className="flex items-center justify-between">
                            <div>
                                <SkeletonBase className="h-3 w-16 mb-2" />
                                <SkeletonBase className="h-8 w-12" />
                            </div>
                            <SkeletonBase className="h-8 w-8 rounded-full" />
                        </div>
                    </div>
                ))}
            </div>
            {/* Дополнительная статистика (мин/макс) */}
            <div className="flex flex-wrap items-center gap-4 mb-6 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                <SkeletonBase className="h-4 w-24" />
                <SkeletonBase className="h-4 w-24" />
                <div className="flex-1">
                    <SkeletonBase className="h-1.5 w-full" />
                </div>
            </div>
            {/* Таблица */}
            <SkeletonTable rows={4} columns={7} />
        </div>
    );
};

/**
 * Скелетон для страницы деталей машины.
 * Включает заглушки для карточки информации, статистики и списка замеров.
 */
export const SkeletonDetails: React.FC = () => {
    return (
        <div>
            {/* Хлебные крошки */}
            <div className="flex items-center gap-2 mb-4">
                <SkeletonBase className="h-4 w-16" />
                <SkeletonBase className="h-4 w-4" />
                <SkeletonBase className="h-4 w-24" />
            </div>

            {/* Заголовок */}
            <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
                <div className="flex items-center gap-3">
                    <SkeletonBase className="h-12 w-12 rounded-lg" />
                    <div>
                        <SkeletonBase className="h-6 w-40 mb-1" />
                        <SkeletonBase className="h-4 w-32" />
                    </div>
                </div>
                <SkeletonBase className="h-8 w-28 rounded-full" />
            </div>

            {/* Карточка информации */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 p-6 mb-6">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-4">
                    {Array.from({ length: 9 }).map((_, i) => (
                        <div key={i} className="flex items-start gap-2">
                            <SkeletonBase className="h-4 w-4 mt-0.5" />
                            <div>
                                <SkeletonBase className="h-3 w-20 mb-1" />
                                <SkeletonBase className="h-4 w-32" />
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Статистика */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 mb-6">
                {Array.from({ length: 4 }).map((_, i) => (
                    <div key={i} className="flex items-center gap-3">
                        <SkeletonBase className="h-10 w-10 rounded-full" />
                        <div>
                            <SkeletonBase className="h-3 w-24 mb-1" />
                            <SkeletonBase className="h-6 w-12" />
                        </div>
                    </div>
                ))}
            </div>

            {/* Список замеров */}
            <div className="mt-6">
                <div className="flex justify-between items-center mb-4">
                    <SkeletonBase className="h-6 w-40" />
                    <SkeletonBase className="h-8 w-32 rounded-lg" />
                </div>
                <SkeletonTable rows={3} columns={6} />
            </div>
        </div>
    );
};

/**
 * Скелетон для списка замеров (используется в раскрывающихся блоках).
 * @param compact - если true, показывает меньше строк.
 */
export const SkeletonMeasurementsList: React.FC<{ compact?: boolean }> = ({ compact = false }) => {
    const rows = compact ? 3 : 5;
    return (
        <div className="py-2">
            <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            {Array.from({ length: 5 }).map((_, i) => (
                                <th key={i} className="px-3 py-2">
                                    <SkeletonBase className="h-3 w-16" />
                                </th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {Array.from({ length: rows }).map((_, rowIdx) => (
                            <tr key={rowIdx}>
                                {Array.from({ length: 5 }).map((_, colIdx) => (
                                    <td key={colIdx} className="px-3 py-2">
                                        <SkeletonBase className="h-3 w-full" />
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            {/* Пагинация (заглушка) */}
            <div className="flex justify-between items-center mt-4">
                <SkeletonBase className="h-4 w-32" />
                <div className="flex gap-1">
                    {Array.from({ length: 4 }).map((_, i) => (
                        <SkeletonBase key={i} className="h-8 w-8 rounded-md" />
                    ))}
                </div>
            </div>
        </div>
    );
};