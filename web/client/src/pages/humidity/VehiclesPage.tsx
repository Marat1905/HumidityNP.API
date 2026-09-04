import { useNavigate, useSearchParams } from 'react-router-dom';
import { useVehicles } from '../../hooks/humidity/useVehicles';
import Pagination from '../../components/common/Pagination';
import { SkeletonTable } from '../../components/common/Skeleton';
import { useState, useEffect, useMemo } from 'react';
import type { VehiclesQueryParams, VehicleDto } from '../../types/humidity';
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
    Package,
    Weight,
    Hash,
    Droplet,
    Table as TableIcon,
    LayoutGrid,
    Calendar,
    Car,
    Gauge,
    Users,
    FileText,
    Box,
    BarChart3,
    TrendingUp,
    TrendingDown,
} from 'lucide-react';
import { measurementService } from '../../services/humidity/api';
import toast from 'react-hot-toast';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';

// Улучшенный компонент карточки для одной машины
function VehicleCard({ vehicle, averageHumidity, isLoadingAvg }: {
    vehicle: VehicleDto;
    averageHumidity: number | null | undefined;
    isLoadingAvg: boolean;
}) {
    const navigate = useNavigate();
    const handleClick = () => {
        navigate(`/humidity/vehicles/${vehicle.id}`);
    };

    const isActive = !vehicle.exitDate;

    // Форматирование дат для отображения
    const formatDate = (dateStr: string) => {
        return format(new Date(dateStr), 'dd.MM.yyyy HH:mm', { locale: ru });
    };

    // Определяем цвет для средней влажности
    const getHumidityColor = (value: number | null | undefined) => {
        if (value === null || value === undefined) return 'text-gray-400';
        if (value < 10) return 'text-green-600 dark:text-green-400';
        if (value < 15) return 'text-yellow-600 dark:text-yellow-400';
        return 'text-red-600 dark:text-red-400';
    };

    // Иконка статуса
    const StatusIcon = isActive ? Clock : BadgeCheck;
    const statusColor = isActive
        ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
        : 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300';

    return (
        <div
            onClick={handleClick}
            className="group bg-white dark:bg-gray-800 rounded-2xl shadow-md border border-gray-200 dark:border-gray-700 p-6 hover:shadow-2xl transition-all duration-300 cursor-pointer hover:border-blue-400 dark:hover:border-blue-600 hover:-translate-y-1"
        >
            {/* Шапка карточки: номер + статус */}
            <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                    <div className="p-2.5 bg-gradient-to-br from-blue-500 to-blue-600 dark:from-blue-600 dark:to-blue-700 rounded-xl shadow-md">
                        <Truck className="w-6 h-6 text-white" />
                    </div>
                    <div>
                        <div className="text-xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
                            {vehicle.number}
                            <span className="text-sm font-normal text-gray-400 dark:text-gray-500">
                                {vehicle.vehiclePlate}
                            </span>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                            <Building2 className="w-4 h-4" />
                            <span>{vehicle.counterparty}</span>
                            {vehicle.inn && (
                                <>
                                    <span className="w-1 h-1 bg-gray-300 dark:bg-gray-600 rounded-full"></span>
                                    <span className="text-xs">ИНН: {vehicle.inn}</span>
                                </>
                            )}
                        </div>
                    </div>
                </div>
                <div className="flex items-center gap-2">
                    <span
                        className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold ${statusColor}`}
                    >
                        <StatusIcon className="w-3.5 h-3.5" />
                        {isActive ? 'На площадке' : 'Выехал'}
                    </span>
                </div>
            </div>

            {/* Основная информация — 3 колонки */}
            <div className="mt-5 grid grid-cols-1 md:grid-cols-3 gap-x-6 gap-y-3">
                {/* Левая колонка: Транспорт и водитель */}
                <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <Car className="w-4 h-4 text-blue-500" />
                        <span className="font-medium">Марка:</span>
                        <span>{vehicle.vehicleBrand}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <Gauge className="w-4 h-4 text-blue-500" />
                        <span className="font-medium">Прицеп:</span>
                        <span>{vehicle.trailer || '—'}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <User className="w-4 h-4 text-blue-500" />
                        <span className="font-medium">Водитель:</span>
                        <span>{vehicle.driver}</span>
                    </div>
                </div>

                {/* Средняя колонка: Даты въезда/выезда */}
                <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <Calendar className="w-4 h-4 text-green-500" />
                        <span className="font-medium">Въезд:</span>
                        <span>{formatDate(vehicle.entryDate)}</span>
                    </div>
                    {vehicle.exitDate && (
                        <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                            <Calendar className="w-4 h-4 text-red-500" />
                            <span className="font-medium">Выезд:</span>
                            <span>{formatDate(vehicle.exitDate)}</span>
                        </div>
                    )}
                    {!vehicle.exitDate && (
                        <div className="flex items-center gap-2 text-sm text-gray-400 dark:text-gray-500">
                            <Clock className="w-4 h-4" />
                            <span>Выезд не зафиксирован</span>
                        </div>
                    )}
                </div>

                {/* Правая колонка: Статистика замеров */}
                <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <BarChart3 className="w-4 h-4 text-purple-500" />
                        <span className="font-medium">Замеров:</span>
                        <span className="font-bold text-gray-900 dark:text-white">{vehicle.measurementsCount}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
                        <Droplet className="w-4 h-4 text-blue-500" />
                        <span className="font-medium">Средняя влажность:</span>
                        {isLoadingAvg ? (
                            <span className="inline-block w-16 h-5 bg-gray-200 dark:bg-gray-700 rounded animate-pulse"></span>
                        ) : averageHumidity !== undefined && averageHumidity !== null ? (
                            <span className={`font-bold ${getHumidityColor(averageHumidity)}`}>
                                {averageHumidity.toFixed(1)}%
                            </span>
                        ) : (
                            <span className="text-gray-400">—</span>
                        )}
                    </div>
                    {/* Добавим индикатор тренда, если есть данные */}
                    {averageHumidity !== undefined && averageHumidity !== null && (
                        <div className="flex items-center gap-2 text-xs">
                            {averageHumidity < 10 ? (
                                <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                                    <TrendingDown className="w-3.5 h-3.5" />
                                    <span>Низкая влажность</span>
                                </span>
                            ) : averageHumidity < 15 ? (
                                <span className="flex items-center gap-1 text-yellow-600 dark:text-yellow-400">
                                    <TrendingUp className="w-3.5 h-3.5" />
                                    <span>Средняя влажность</span>
                                </span>
                            ) : (
                                <span className="flex items-center gap-1 text-red-600 dark:text-red-400">
                                    <TrendingUp className="w-3.5 h-3.5" />
                                    <span>Высокая влажность</span>
                                </span>
                            )}
                        </div>
                    )}
                </div>
            </div>

            {/* Блок разгрузки — отдельная секция с иконками */}
            <div className="mt-5 pt-4 border-t border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2 mb-2 text-xs font-medium text-gray-400 dark:text-gray-500 uppercase tracking-wider">
                    <Package className="w-4 h-4" />
                    <span>Данные разгрузки</span>
                </div>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                    <div className="flex items-center gap-2 text-sm bg-gray-50 dark:bg-gray-700/50 rounded-lg px-3 py-2">
                        <Package className="w-4 h-4 text-indigo-500" />
                        <div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">Тюки</div>
                            <div className="font-semibold text-gray-900 dark:text-white">
                                {vehicle.baleCount != null ? vehicle.baleCount : '—'}
                            </div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 text-sm bg-gray-50 dark:bg-gray-700/50 rounded-lg px-3 py-2">
                        <Package className="w-4 h-4 text-red-500" />
                        <div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">Порванные</div>
                            <div className="font-semibold text-gray-900 dark:text-white">
                                {vehicle.damagedBaleCount != null ? vehicle.damagedBaleCount : '—'}
                            </div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 text-sm bg-gray-50 dark:bg-gray-700/50 rounded-lg px-3 py-2">
                        <Weight className="w-4 h-4 text-green-500" />
                        <div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">Вес (кг)</div>
                            <div className="font-semibold text-gray-900 dark:text-white">
                                {vehicle.weightKg != null ? vehicle.weightKg : '—'}
                            </div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 text-sm bg-gray-50 dark:bg-gray-700/50 rounded-lg px-3 py-2">
                        <Hash className="w-4 h-4 text-purple-500" />
                        <div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">Штабель</div>
                            <div className="font-semibold text-gray-900 dark:text-white">
                                {vehicle.stackNumber || '—'}
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Подсказка при наведении */}
            <div className="mt-3 text-xs text-gray-400 dark:text-gray-500 text-right opacity-0 group-hover:opacity-100 transition-opacity">
                Нажмите для просмотра деталей →
            </div>
        </div>
    );
}

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

    const [viewMode, setViewMode] = useState<'table' | 'cards'>(() => {
        const saved = localStorage.getItem('vehicles_view_mode');
        return (saved === 'cards' || saved === 'table') ? saved : 'table';
    });

    const [averageHumidityMap, setAverageHumidityMap] = useState<Record<string, number | null>>({});
    const [loadingStats, setLoadingStats] = useState<Record<string, boolean>>({});

    useEffect(() => {
        setLocalCounterparty(counterparty);
        setLocalStatus(status);
        setLocalPlate(plate);
        setLocalDriver(driver);
    }, [counterparty, status, plate, driver]);

    useEffect(() => {
        localStorage.setItem('vehicles_filters_collapsed', JSON.stringify(filtersCollapsed));
    }, [filtersCollapsed]);

    useEffect(() => {
        localStorage.setItem('vehicles_view_mode', viewMode);
    }, [viewMode]);

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

    const { data, loading, error, refetch } = useVehicles(queryParams);

    useEffect(() => {
        // ИСПРАВЛЕНИЕ: добавлена опциональная цепочка ?. для безопасной проверки длины
        if (!data || !data.items?.length) {
            setAverageHumidityMap({});
            setLoadingStats({});
            return;
        }

        const vehicleIds = data.items.map(v => v.id);
        const newLoadingStats: Record<string, boolean> = {};
        vehicleIds.forEach(id => {
            if (!(id in averageHumidityMap)) {
                newLoadingStats[id] = true;
            }
        });
        setLoadingStats(prev => ({ ...prev, ...newLoadingStats }));

        vehicleIds.forEach(id => {
            if (id in averageHumidityMap) return;
            measurementService.getStatisticsByVehicle(id)
                .then(stats => {
                    setAverageHumidityMap(prev => ({
                        ...prev,
                        [id]: stats.average,
                    }));
                    setLoadingStats(prev => {
                        const newState = { ...prev };
                        delete newState[id];
                        return newState;
                    });
                })
                .catch(err => {
                    console.error(`Ошибка загрузки статистики для машины ${id}`, err);
                    setAverageHumidityMap(prev => ({
                        ...prev,
                        [id]: null,
                    }));
                    setLoadingStats(prev => {
                        const newState = { ...prev };
                        delete newState[id];
                        return newState;
                    });
                });
        });
    }, [data, averageHumidityMap]);

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

    const toggleFilters = () => {
        setFiltersCollapsed(prev => !prev);
    };

    if (loading) return <SkeletonTable rows={5} columns={11} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;
    if (!data) return null;

    // ИСПРАВЛЕНИЕ: добавлена защита от undefined для items, totalCount и totalPages
    const items = data.items ?? [];
    const totalCount = data.totalCount ?? 0;
    const totalPages = data.totalPages ?? 0;

    const handleRowClick = (vehicleId: string) => {
        const queryString = searchParams.toString();
        const path = queryString ? `/humidity/vehicles/${vehicleId}?${queryString}` : `/humidity/vehicles/${vehicleId}`;
        navigate(path);
    };

    return (
        <div>
            {/* Блок фильтров со сворачиванием */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-5 mb-6 transition-all">
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
                <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-500 dark:text-gray-400 mr-1">Вид:</span>
                    <button
                        onClick={() => setViewMode('table')}
                        className={`p-2 rounded-lg border transition ${viewMode === 'table'
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                            : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                            }`}
                        aria-label="Табличный вид"
                    >
                        <TableIcon className="w-5 h-5" />
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
            </div>

            {viewMode === 'table' ? (
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
                                    <div className="flex items-center gap-1">
                                        <Droplet className="w-3 h-3" />
                                        Средняя влажность
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    Статус
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1">
                                        <Package className="w-3 h-3" />
                                        Тюки
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1">
                                        <Package className="w-3 h-3" />
                                        Порванные
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1">
                                        <Weight className="w-3 h-3" />
                                        Вес (кг)
                                    </div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                                    <div className="flex items-center gap-1">
                                        <Hash className="w-3 h-3" />
                                        Штабель
                                    </div>
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                            {items.map((vehicle) => {
                                const avg = averageHumidityMap[vehicle.id];
                                const isAvgLoading = loadingStats[vehicle.id];
                                return (
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
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                            {isAvgLoading ? (
                                                <div className="animate-pulse h-4 w-12 bg-gray-200 dark:bg-gray-700 rounded mx-auto"></div>
                                            ) : avg !== undefined && avg !== null ? (
                                                <span className="font-medium text-blue-600 dark:text-blue-400">
                                                    {avg.toFixed(1)}%
                                                </span>
                                            ) : (
                                                <span className="text-gray-400 dark:text-gray-500">—</span>
                                            )}
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
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                            {vehicle.baleCount != null ? vehicle.baleCount : '—'}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                            {vehicle.damagedBaleCount != null ? vehicle.damagedBaleCount : '—'}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                            {vehicle.weightKg != null ? vehicle.weightKg : '—'}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                            {vehicle.stackNumber || '—'}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            ) : (
                <div className="grid grid-cols-1 gap-6">
                    {items.map((vehicle) => {
                        const avg = averageHumidityMap[vehicle.id];
                        const isAvgLoading = loadingStats[vehicle.id];
                        return (
                            <VehicleCard
                                key={vehicle.id}
                                vehicle={vehicle}
                                averageHumidity={avg}
                                isLoadingAvg={isAvgLoading}
                            />
                        );
                    })}
                </div>
            )}

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