import { useNavigate, useSearchParams } from 'react-router-dom';
import { useVehicles } from '../hooks/useVehicles';
import Pagination from '../components/shared/Pagination';
import { SkeletonTable } from '../components/shared/Skeleton';
import { useState, useEffect, useMemo } from 'react';
import type { VehiclesQueryParams } from '../types';
import {
    Search,
    X,
    Truck,
    Building2,
    User,
    BadgeCheck,
    Clock,
    Filter,
    ChevronDown,
    ChevronUp,
} from 'lucide-react';

export default function VehiclesPage() {
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();

    const page = parseInt(searchParams.get('page') || '1', 10);
    const size = parseInt(searchParams.get('size') || '10', 10);
    const counterparty = searchParams.get('counterparty') || '';
    const status = searchParams.get('status') || 'active';
    const plate = searchParams.get('plate') || '';
    const driver = searchParams.get('driver') || '';

    const [localCounterparty, setLocalCounterparty] = useState(counterparty);
    const [localStatus, setLocalStatus] = useState(status);
    const [localPlate, setLocalPlate] = useState(plate);
    const [localDriver, setLocalDriver] = useState(driver);

    const [filtersCollapsed, setFiltersCollapsed] = useState(() => {
        const saved = localStorage.getItem('vehicles_filters_collapsed');
        return saved ? JSON.parse(saved) : false;
    });

    useEffect(() => {
        setLocalCounterparty(counterparty);
        setLocalStatus(status);
        setLocalPlate(plate);
        setLocalDriver(driver);
    }, [counterparty, status, plate, driver]);

    useEffect(() => {
        localStorage.setItem('vehicles_filters_collapsed', JSON.stringify(filtersCollapsed));
    }, [filtersCollapsed]);

    const queryParams: VehiclesQueryParams = useMemo(
        () => ({
            pageNumber: page,
            pageSize: size,
            counterparty: counterparty || undefined,
            status: status as 'active' | 'exited' | 'all' | undefined,
            plate: plate || undefined,
            driver: driver || undefined,
        }),
        [page, size, counterparty, status, plate, driver]
    );

    const { data, loading, error } = useVehicles(queryParams);

    const activeFilters = [];
    if (counterparty) activeFilters.push({ key: 'counterparty', label: `Поставщик: ${counterparty}`, value: counterparty });
    if (plate) activeFilters.push({ key: 'plate', label: `Госномер: ${plate}`, value: plate });
    if (driver) activeFilters.push({ key: 'driver', label: `Водитель: ${driver}`, value: driver });
    if (status !== 'active') {
        const statusLabel = status === 'exited' ? 'Выехали' : 'Все';
        activeFilters.push({ key: 'status', label: `Статус: ${statusLabel}`, value: status });
    }

    const hasActiveFilters = activeFilters.length > 0;

    const clearFilter = (key: string) => {
        if (key === 'counterparty') { setLocalCounterparty(''); }
        if (key === 'plate') { setLocalPlate(''); }
        if (key === 'driver') { setLocalDriver(''); }
        if (key === 'status') { setLocalStatus('active'); }

        setSearchParams({
            page: '1',
            size: String(size),
            counterparty: key === 'counterparty' ? '' : localCounterparty,
            status: key === 'status' ? 'active' : localStatus,
            plate: key === 'plate' ? '' : localPlate,
            driver: key === 'driver' ? '' : localDriver,
        });
    };

    const applyFilters = () => {
        setSearchParams({
            page: '1',
            size: String(size),
            counterparty: localCounterparty,
            status: localStatus,
            plate: localPlate,
            driver: localDriver,
        });
    };

    const resetFilters = () => {
        setLocalCounterparty('');
        setLocalStatus('active');
        setLocalPlate('');
        setLocalDriver('');
        setSearchParams({
            page: '1',
            size: String(size),
            status: 'active',
        });
    };

    const handlePageChange = (newPage: number) => {
        setSearchParams({
            page: String(newPage),
            size: String(size),
            counterparty: localCounterparty,
            status: localStatus,
            plate: localPlate,
            driver: localDriver,
        });
    };

    const handlePageSizeChange = (newSize: number) => {
        setSearchParams({
            page: '1',
            size: String(newSize),
            counterparty: localCounterparty,
            status: localStatus,
            plate: localPlate,
            driver: localDriver,
        });
    };

    // Переключение свёрнутости (вызывается по клику на заголовок)
    const toggleFilters = () => {
        setFiltersCollapsed(prev => !prev);
    };

    if (loading) return <SkeletonTable rows={5} columns={6} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;
    if (!data) return null;

    const { items, totalCount, totalPages } = data;

    const handleRowClick = (vehicleId: string) => {
        const queryString = searchParams.toString();
        const path = queryString ? `/vehicles/${vehicleId}?${queryString}` : `/vehicles/${vehicleId}`;
        navigate(path);
    };

    return (
        <div>
            {/* Блок фильтров со сворачиванием по клику на весь заголовок */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-5 mb-6 transition-all">
                {/* Заголовок фильтров – клик по всей области переключает состояние */}
                <div
                    className="flex items-center justify-between cursor-pointer select-none"
                    onClick={toggleFilters}
                >
                    <div className="flex items-center gap-2">
                        <Filter className="w-5 h-5 text-blue-500" />
                        <span className="text-sm font-semibold text-gray-700 dark:text-gray-300">Фильтры</span>
                        {hasActiveFilters && (
                            <span className="text-xs text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/30 px-2 py-0.5 rounded-full">
                                {activeFilters.length} активных
                            </span>
                        )}
                    </div>
                    <div className="flex items-center gap-3" onClick={(e) => e.stopPropagation()}>
                        {hasActiveFilters && !filtersCollapsed && (
                            <button
                                onClick={(e) => {
                                    e.stopPropagation();
                                    resetFilters();
                                }}
                                className="text-sm text-gray-500 dark:text-gray-400 hover:text-red-500 dark:hover:text-red-400 transition flex items-center gap-1"
                            >
                                <X className="w-4 h-4" />
                                Сбросить все
                            </button>
                        )}
                        <button
                            onClick={(e) => {
                                e.stopPropagation();
                                toggleFilters();
                            }}
                            className="p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition text-gray-500 dark:text-gray-400"
                            aria-label={filtersCollapsed ? 'Развернуть фильтры' : 'Свернуть фильтры'}
                        >
                            {filtersCollapsed ? (
                                <ChevronDown className="w-5 h-5" />
                            ) : (
                                <ChevronUp className="w-5 h-5" />
                            )}
                        </button>
                    </div>
                </div>

                {/* Содержимое фильтров */}
                {!filtersCollapsed && (
                    <div className="mt-4 space-y-4">
                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                            <div className="relative">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <Building2 className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                                </div>
                                <input
                                    type="text"
                                    value={localCounterparty}
                                    onChange={(e) => setLocalCounterparty(e.target.value)}
                                    placeholder="Поставщик"
                                    className="w-full pl-9 pr-8 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                                />
                                {localCounterparty && (
                                    <button
                                        onClick={() => setLocalCounterparty('')}
                                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                                    >
                                        <X className="w-4 h-4" />
                                    </button>
                                )}
                            </div>

                            <div className="relative">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <Truck className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                                </div>
                                <input
                                    type="text"
                                    value={localPlate}
                                    onChange={(e) => setLocalPlate(e.target.value)}
                                    placeholder="Госномер"
                                    className="w-full pl-9 pr-8 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                                />
                                {localPlate && (
                                    <button
                                        onClick={() => setLocalPlate('')}
                                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                                    >
                                        <X className="w-4 h-4" />
                                    </button>
                                )}
                            </div>

                            <div className="relative">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <User className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                                </div>
                                <input
                                    type="text"
                                    value={localDriver}
                                    onChange={(e) => setLocalDriver(e.target.value)}
                                    placeholder="Водитель"
                                    className="w-full pl-9 pr-8 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                                />
                                {localDriver && (
                                    <button
                                        onClick={() => setLocalDriver('')}
                                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                                    >
                                        <X className="w-4 h-4" />
                                    </button>
                                )}
                            </div>

                            <div className="flex items-center gap-2">
                                <button
                                    onClick={applyFilters}
                                    className="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition shadow-sm flex items-center justify-center gap-2"
                                >
                                    <Search className="w-4 h-4" />
                                    Поиск
                                </button>
                            </div>
                        </div>

                        <div className="flex flex-wrap items-center gap-3 pt-4 border-t border-gray-100 dark:border-gray-700">
                            <span className="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-1">
                                <Clock className="w-4 h-4" />
                                Статус:
                            </span>
                            <div className="flex flex-wrap gap-2">
                                {[
                                    { value: 'active', label: 'На площадке', icon: <Clock className="w-4 h-4" />, color: 'yellow' },
                                    { value: 'exited', label: 'Выехали', icon: <BadgeCheck className="w-4 h-4" />, color: 'green' },
                                    { value: 'all', label: 'Все', icon: <Filter className="w-4 h-4" />, color: 'gray' },
                                ].map((opt) => {
                                    const isActive = localStatus === opt.value;
                                    const bgColor = isActive
                                        ? opt.color === 'yellow' ? 'bg-yellow-100 dark:bg-yellow-900/40 border-yellow-300 dark:border-yellow-700 text-yellow-800 dark:text-yellow-200'
                                            : opt.color === 'green' ? 'bg-green-100 dark:bg-green-900/40 border-green-300 dark:border-green-700 text-green-800 dark:text-green-200'
                                                : 'bg-gray-100 dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300'
                                        : 'bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700';

                                    return (
                                        <button
                                            key={opt.value}
                                            onClick={() => setLocalStatus(opt.value)}
                                            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm font-medium border transition-all duration-200 ${bgColor} ${isActive ? 'shadow-sm ring-2 ring-offset-1 ring-blue-500 dark:ring-offset-gray-800' : ''
                                                }`}
                                        >
                                            {opt.icon}
                                            {opt.label}
                                        </button>
                                    );
                                })}
                            </div>
                        </div>

                        {hasActiveFilters && (
                            <div className="flex flex-wrap gap-2 pt-4 border-t border-gray-100 dark:border-gray-700">
                                {activeFilters.map((filter) => (
                                    <span
                                        key={filter.key}
                                        className="inline-flex items-center gap-1 px-3 py-1 bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-sm rounded-full border border-blue-200 dark:border-blue-800"
                                    >
                                        {filter.label}
                                        <button
                                            onClick={() => clearFilter(filter.key)}
                                            className="hover:text-red-500 transition"
                                        >
                                            <X className="w-3.5 h-3.5" />
                                        </button>
                                    </span>
                                ))}
                            </div>
                        )}
                    </div>
                )}

                {filtersCollapsed && hasActiveFilters && (
                    <div className="mt-2 flex flex-wrap gap-2">
                        {activeFilters.slice(0, 3).map((filter) => (
                            <span
                                key={filter.key}
                                className="inline-flex items-center gap-1 px-2 py-0.5 bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-xs rounded-full border border-blue-200 dark:border-blue-800"
                            >
                                {filter.label}
                            </span>
                        ))}
                        {activeFilters.length > 3 && (
                            <span className="text-xs text-gray-500 dark:text-gray-400">
                                +{activeFilters.length - 3} ещё
                            </span>
                        )}
                    </div>
                )}
            </div>

            <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
                    Машины
                    <span className="ml-2 text-sm font-normal text-gray-500 dark:text-gray-400">
                        ({totalCount} записей)
                    </span>
                </h2>
            </div>

            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Номер пропуска
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Поставщик
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Гос. номер
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Водитель
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Замеры
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                Статус
                            </th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {items.map((vehicle) => (
                            <tr
                                key={vehicle.id}
                                onClick={() => handleRowClick(vehicle.id)}
                                className="hover:bg-gray-50 dark:hover:bg-gray-800 transition cursor-pointer"
                                role="button"
                                tabIndex={0}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' || e.key === ' ') {
                                        e.preventDefault();
                                        handleRowClick(vehicle.id);
                                    }
                                }}
                            >
                                <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">
                                    {vehicle.number}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {vehicle.counterparty}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {vehicle.vehiclePlate}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                                    {vehicle.driver}
                                </td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                    {vehicle.measurementsCount}
                                </td>
                                <td className="px-4 py-3 text-sm">
                                    <span
                                        className={`px-2 py-1 rounded-full text-xs font-medium ${vehicle.exitDate
                                            ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
                                            : 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
                                            }`}
                                    >
                                        {vehicle.exitDate ? 'Выехал' : 'На площадке'}
                                    </span>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            <Pagination
                currentPage={page}
                totalPages={totalPages}
                onPageChange={handlePageChange}
                pageSize={size}
                onPageSizeChange={handlePageSizeChange}
                totalCount={totalCount}
            />
        </div>
    );
}