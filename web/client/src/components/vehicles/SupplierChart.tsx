import React from 'react';
import {
    ComposedChart,
    Bar,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
    Scatter,
    Cell,
} from 'recharts';
import type { SupplierVehicleSummaryDto } from '../../types';

interface SupplierChartProps {
    vehicles: SupplierVehicleSummaryDto[];
}

/**
 * Компонент графика для поставщика.
 * Отображает диапазон влажности (мин–макс) в виде столбцов и среднее значение точкой.
 */
const SupplierChart: React.FC<SupplierChartProps> = ({ vehicles }) => {
    // Подготовка данных: для каждой машины – min, max, среднее, кол-во замеров
    const chartData = vehicles
        .map(v => ({
            // Для оси X используем номер машины или госномер
            name: v.number || v.vehiclePlate || v.vehicleId.slice(0, 8),
            minHumidity: v.minHumidity ?? 0,
            maxHumidity: v.maxHumidity ?? 0,
            averageHumidity: v.averageHumidity ?? 0,
            measurementsCount: v.measurementsCount,
            vehicleId: v.vehicleId,
            range: [v.minHumidity ?? 0, v.maxHumidity ?? 0] as [number, number],
        }))
        .filter(d => d.minHumidity !== null && d.maxHumidity !== null) // отфильтровываем машины без данных
        .sort((a, b) => b.measurementsCount - a.measurementsCount);

    if (chartData.length === 0) {
        return <div className="text-center text-gray-500 dark:text-gray-400">Нет данных для графика</div>;
    }

    // Цветовая палитра для столбцов
    const colors = ['#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6', '#f59e0b', '#ef4444', '#6366f1', '#22d3ee'];

    // Максимальное значение для оси Y (с запасом 10%)
    const maxValue = Math.max(...chartData.map(d => d.maxHumidity), 0) * 1.1 || 100;

    /**
     * Кастомный тултип, который отображает только нужные поля:
     * - Название машины (label)
     * - Диапазон (мин–макс)
     * - Среднее значение
     */
    const CustomTooltip = ({ active, payload, label }: any) => {
        if (!active || !payload || payload.length === 0) return null;

        // Ищем нужные данные
        const rangePayload = payload.find((p: any) => p.dataKey === 'range');
        const avgPayload = payload.find((p: any) => p.dataKey === 'averageHumidity');

        return (
            <div className="bg-gray-800 text-white p-3 rounded-lg shadow-lg border border-gray-600 text-sm">
                <div className="font-semibold text-gray-200 mb-1">Машина: {label}</div>
                {rangePayload && (
                    <div className="flex justify-between gap-4">
                        <span className="text-gray-400">Диапазон:</span>
                        <span className="font-medium text-blue-400">
                            {rangePayload.payload.minHumidity.toFixed(1)}% – {rangePayload.payload.maxHumidity.toFixed(1)}%
                        </span>
                    </div>
                )}
                {avgPayload && (
                    <div className="flex justify-between gap-4">
                        <span className="text-gray-400">Средняя:</span>
                        <span className="font-medium text-red-400">
                            {avgPayload.payload.averageHumidity.toFixed(1)}%
                        </span>
                    </div>
                )}
            </div>
        );
    };

    return (
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 p-4">
            <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                Диапазон влажности по машинам
            </h4>
            <ResponsiveContainer width="100%" height={300}>
                <ComposedChart data={chartData} margin={{ top: 20, right: 30, left: 0, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                    <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#9ca3af' }} />
                    <YAxis
                        domain={[0, Math.ceil(maxValue / 10) * 10]}
                        tick={{ fontSize: 12, fill: '#9ca3af' }}
                        unit="%"
                    />
                    {/* Используем кастомный тултип вместо стандартного */}
                    <Tooltip content={<CustomTooltip />} />
                    <Legend
                        formatter={(value: string) => {
                            // Переименовываем легенду для понятности
                            if (value === 'range') return 'Диапазон влажности';
                            if (value === 'averageHumidity') return 'Среднее значение';
                            return value;
                        }}
                    />
                    {/* Столбцы диапазона */}
                    <Bar dataKey="range" name="range" fill="#3b82f6" barSize={16}>
                        {chartData.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={colors[index % colors.length]} opacity={0.6} />
                        ))}
                    </Bar>
                    {/* Точки среднего значения */}
                    <Scatter
                        dataKey="averageHumidity"
                        name="averageHumidity"
                        fill="#ef4444"
                        shape="circle"
                        legendType="circle"
                        line={false}
                    >
                        {chartData.map((entry, index) => (
                            <Cell key={`scatter-${index}`} fill="#ef4444" />
                        ))}
                    </Scatter>
                </ComposedChart>
            </ResponsiveContainer>
            <div className="mt-2 text-xs text-gray-500 dark:text-gray-400 text-center">
                Отображён диапазон влажности (мин–макс) для каждой машины за выбранный период. Красная точка — среднее значение.
            </div>
        </div>
    );
};

export default SupplierChart;