import React from 'react';
import {
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
    BarChart,
    Bar,
    Cell,
} from 'recharts';
import type { SupplierVehicleSummaryDto } from '../../types';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';

interface SupplierChartProps {
    vehicles: SupplierVehicleSummaryDto[];
}

const SupplierChart: React.FC<SupplierChartProps> = ({ vehicles }) => {
    // Подготовка данных: для каждой машины – средняя влажность и количество замеров
    const chartData = vehicles.map(v => ({
        name: v.number || v.vehiclePlate || v.vehicleId.slice(0, 8),
        averageHumidity: v.averageHumidity ?? 0,
        measurementsCount: v.measurementsCount,
        vehicleId: v.vehicleId,
    })).sort((a, b) => b.measurementsCount - a.measurementsCount);

    if (chartData.length === 0) {
        return <div className="text-center text-gray-500 dark:text-gray-400">Нет данных для графика</div>;
    }

    // Цвета для баров
    const colors = ['#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6', '#f59e0b', '#ef4444', '#6366f1', '#22d3ee'];

    return (
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 p-4">
            <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                Средняя влажность по машинам
            </h4>
            <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData} margin={{ top: 20, right: 30, left: 0, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                    <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#9ca3af' }} />
                    <YAxis domain={[0, 100]} tick={{ fontSize: 12, fill: '#9ca3af' }} unit="%" />
                    <Tooltip
                        formatter={(value: number) => value.toFixed(1) + '%'}
                        labelFormatter={(label) => `Машина: ${label}`}
                        contentStyle={{ backgroundColor: '#1f2937', color: '#f3f4f6', border: 'none' }}
                    />
                    <Legend />
                    <Bar dataKey="averageHumidity" name="Средняя влажность" fill="#3b82f6">
                        {chartData.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={colors[index % colors.length]} />
                        ))}
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
            <div className="mt-2 text-xs text-gray-500 dark:text-gray-400 text-center">
                Отображена средняя влажность по каждой машине за выбранный период. Количество замеров: {chartData.reduce((sum, d) => sum + d.measurementsCount, 0)}
            </div>
        </div>
    );
};

export default SupplierChart;