import React from 'react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { StackSessionDto } from '../../../types/humidity';
import { TrendingDown, TrendingUp } from 'lucide-react';

interface StackSessionsTableProps {
    sessions: StackSessionDto[];
}

/**
 * Плотная таблица сессий по штабелям.
 * Альтернативный вид — для случаев, когда нужно «увидеть всё сразу».
 *
 * Влажности nullable: null → «—», а не «0.00%».
 */
const StackSessionsTable: React.FC<StackSessionsTableProps> = ({ sessions }) => {
    const formatSessionDate = (iso: string) =>
        format(new Date(iso), 'dd.MM.yyyy HH:mm', { locale: ru });

    return (
        <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-700">
                    <tr>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Штабель</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Сессия</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Начало</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Конец</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Длит.</th>
                        <th className="px-3 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Замеров</th>
                        <th className="px-3 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Машин</th>
                        <th className="px-3 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Поставщ.</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Основной поставщик</th>
                        <th className="px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Все поставщики</th>
                        <th className="px-3 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ср.</th>
                        <th className="px-3 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Мин / Макс</th>
                    </tr>
                </thead>
                <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                    {sessions.map((s) => (
                        <tr
                            key={`${s.stackNumber}-${s.sessionId}`}
                            className="hover:bg-gray-50 dark:hover:bg-gray-800 transition"
                        >
                            <td className="px-3 py-2.5 text-sm font-semibold text-gray-900 dark:text-white whitespace-nowrap">
                                {s.stackNumber}
                            </td>
                            <td className="px-3 py-2.5 text-sm text-gray-700 dark:text-gray-300">#{s.sessionId}</td>
                            <td className="px-3 py-2.5 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                {formatSessionDate(s.sessionStart)}
                            </td>
                            <td className="px-3 py-2.5 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                {formatSessionDate(s.sessionEnd)}
                            </td>
                            <td className="px-3 py-2.5 text-sm font-mono text-gray-700 dark:text-gray-300 whitespace-nowrap">
                                {s.duration}
                            </td>
                            <td className="px-3 py-2.5 text-sm text-center text-gray-700 dark:text-gray-300">{s.measurementsCount}</td>
                            <td className="px-3 py-2.5 text-sm text-center text-gray-700 dark:text-gray-300">{s.vehiclesCount}</td>
                            <td className="px-3 py-2.5 text-sm text-center text-gray-700 dark:text-gray-300">{s.counterpartyCount}</td>
                            <td className="px-3 py-2.5 text-sm text-gray-900 dark:text-white font-medium">
                                {s.primaryCounterparty || '—'}
                            </td>
                            <td className="px-3 py-2.5 text-xs text-gray-600 dark:text-gray-400 max-w-md">
                                {s.counterparties || '—'}
                            </td>
                            <td className="px-3 py-2.5 text-sm text-center font-semibold text-gray-900 dark:text-white whitespace-nowrap">
                                {s.averageHumidity !== null ? s.averageHumidity.toFixed(2) + '%' : '—'}
                            </td>
                            <td className="px-3 py-2.5 text-sm text-center whitespace-nowrap">
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
    );
};

export default StackSessionsTable;