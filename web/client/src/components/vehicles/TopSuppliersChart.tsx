import React, { useMemo } from 'react';
import {
    ComposedChart,
    Bar,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
    Cell,
} from 'recharts';
import type { SupplierDto } from '../../types';
import { Droplet, Activity, Truck } from 'lucide-react';

interface TopSuppliersChartProps {
    suppliers: SupplierDto[];
    rankType: 'good' | 'bad';
    maxItems?: number;
}

const TopSuppliersChart: React.FC<TopSuppliersChartProps> = ({
    suppliers,
    rankType,
    maxItems = 20,
}) => {
    const displayData = useMemo(() => suppliers.slice(0, maxItems), [suppliers, maxItems]);

    if (displayData.length === 0) {
        return (
            <div className="text-center text-gray-500 dark:text-gray-400 py-12 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700">
                <Droplet className="w-12 h-12 mx-auto text-gray-300 dark:text-gray-600 mb-2" />
                <p>Нет данных для отображения</p>
            </div>
        );
    }

    const chartData = displayData.map((supplier) => ({
        fullName: supplier.counterparty,
        shortName: supplier.counterparty.length > 15
            ? supplier.counterparty.slice(0, 14) + '…'
            : supplier.counterparty,
        inn: supplier.inn,
        min: supplier.minHumidity ?? 0,
        max: supplier.maxHumidity ?? 0,
        average: supplier.averageHumidity ?? 0,
        range: [supplier.minHumidity ?? 0, supplier.maxHumidity ?? 0],
        measurements: supplier.totalMeasurements,
        vehicles: supplier.vehiclesCount,
    }));

    const getBarColor = (index: number, total: number) => {
        const ratio = index / total;
        if (rankType === 'good') {
            const intensity = 1 - ratio * 0.6;
            return `rgba(16, 185, 129, ${intensity})`;
        } else {
            const intensity = 1 - ratio * 0.5;
            return `rgba(239, 68, 68, ${intensity})`;
        }
    };

    const barColor = rankType === 'good' ? '#10b981' : '#ef4444';

    const CustomTooltip = ({ active, payload }: any) => {
        if (!active || !payload || payload.length === 0) return null;
        const data = payload[0].payload;
        return (
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl shadow-2xl p-4 max-w-xs">
                <div className="flex items-start justify-between">
                    <div>
                        <div className="font-bold text-gray-900 dark:text-white text-base">
                            {data.fullName}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                            ИНН: {data.inn}
                        </div>
                    </div>
                    <div className={`px-2 py-1 rounded-full text-xs font-semibold ${rankType === 'good'
                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
                        : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
                        }`}>
                        {rankType === 'good' ? 'Лучший' : 'Худший'}
                    </div>
                </div>
                <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5 text-sm">
                    <div className="flex items-center gap-1.5 col-span-2">
                        <span className="text-gray-600 dark:text-gray-300">Диапазон влажности:</span>
                        <span className="font-medium text-gray-800 dark:text-gray-200">
                            {data.min.toFixed(1)}% – {data.max.toFixed(1)}%
                        </span>
                    </div>
                    <div className="flex items-center gap-1.5">
                        <Droplet className="w-4 h-4 text-blue-500" />
                        <span className="text-gray-600 dark:text-gray-300">Средняя:</span>
                        <span className="font-bold text-gray-900 dark:text-white">
                            {data.average.toFixed(1)}%
                        </span>
                    </div>
                    <div className="flex items-center gap-1.5">
                        <Activity className="w-4 h-4 text-indigo-500" />
                        <span className="text-gray-600 dark:text-gray-300">Замеров:</span>
                        <span className="font-medium text-gray-900 dark:text-white">
                            {data.measurements}
                        </span>
                    </div>
                    <div className="flex items-center gap-1.5 col-span-2">
                        <Truck className="w-4 h-4 text-blue-500" />
                        <span className="text-gray-600 dark:text-gray-300">Машин:</span>
                        <span className="font-medium text-gray-900 dark:text-white">
                            {data.vehicles}
                        </span>
                    </div>
                </div>
            </div>
        );
    };

    const maxValue = Math.max(...chartData.map(d => d.max), 0) * 1.15 || 100;
    const minValue = Math.min(...chartData.map(d => d.min), 0) * 0.85 || 0;

    return (
        <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-200 dark:border-gray-700 p-6 transition-all hover:shadow-xl">
            <ResponsiveContainer width="100%" height={400}>
                <ComposedChart
                    data={chartData}
                    margin={{ top: 20, right: 30, left: 0, bottom: 10 }}
                    barCategoryGap="20%"
                >
                    <CartesianGrid
                        strokeDasharray="4 4"
                        stroke="#e5e7eb"
                        vertical={false}
                        className="dark:stroke-gray-700"
                    />
                    <XAxis
                        dataKey="shortName"
                        angle={-45}
                        textAnchor="end"
                        interval={0}
                        height={80}
                        tick={{ fontSize: 11, fill: '#6b7280' }}
                        className="dark:fill-gray-400"
                    />
                    <YAxis
                        domain={[Math.max(0, minValue - 5), maxValue + 5]}
                        tick={{ fontSize: 12, fill: '#6b7280' }}
                        unit="%"
                        tickFormatter={(value) => Number(value).toFixed(0)}
                        className="dark:fill-gray-400"
                    />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(0,0,0,0.05)' }} />
                    <Legend
                        verticalAlign="top"
                        height={36}
                        iconType="circle"
                        formatter={(value) => {
                            if (value === 'range') return 'Диапазон влажности (мин–макс)';
                            if (value === 'average') return 'Среднее значение';
                            return value;
                        }}
                    />

                    <Bar
                        dataKey="range"
                        name="range"
                        barSize={16}
                        fill={barColor}
                        animationDuration={800}
                    >
                        {chartData.map((entry, index) => (
                            <Cell
                                key={`cell-${index}`}
                                fill={getBarColor(index, chartData.length)}
                                opacity={0.7}
                            />
                        ))}
                    </Bar>

                    <Line
                        type="monotone"
                        dataKey="average"
                        name="average"
                        stroke="#f59e0b"
                        strokeWidth={2}
                        dot={{ r: 4, fill: '#f59e0b' }}
                        activeDot={{ r: 6 }}
                    />
                </ComposedChart>
            </ResponsiveContainer>

            <div className="mt-3 flex flex-wrap items-center justify-between text-xs text-gray-500 dark:text-gray-400">
                <span>Столбцы – диапазон влажности (мин–макс), линия с точками – среднее значение.</span>
                <span className="flex items-center gap-2">
                    <span className="inline-block w-3 h-3 rounded-full bg-gradient-to-r from-green-400 to-red-400" />
                    Цвет столбцов зависит от позиции в топе
                </span>
            </div>
        </div>
    );
};

export default TopSuppliersChart;