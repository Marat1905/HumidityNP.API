import { useState, useEffect } from 'react';
import { useParams, Link, useSearchParams } from 'react-router-dom';
import {
    ArrowLeft,
    Truck,
    Calendar,
    User,
    Building2,
    ClipboardList,
    Car,
    Gauge,
    Users,
    Briefcase,
    Clock,
    CheckCircle,
    XCircle,
    FileText,
    Package, // иконка для тюков
    Weight, // иконка для веса
    Hash, // иконка для номера штабеля
} from 'lucide-react';
import toast from 'react-hot-toast';
import { vehicleService } from '../../services/humidity/api';
import { useMeasurements } from '../../hooks/humidity/useMeasurements';
import { useMeasurementStatistics } from '../../hooks/humidity/useMeasurementStatistics';
import MeasurementList from '../../components/humidity/MeasurementList';
import MeasurementStatistics from '../../components/humidity/MeasurementStatistics';
import { SkeletonDetails, SkeletonMeasurementsList } from '../../components/common/Skeleton';

export default function VehicleDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const [searchParams] = useSearchParams();
    const [vehicle, setVehicle] = useState<any>(null);
    const [loadingVehicle, setLoadingVehicle] = useState(true);

    // Состояние пагинации замеров
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    const { data: measurementsData, loading: loadingMeasurements, refetch: refetchMeasurements } = useMeasurements(
        id || null,
        pageNumber,
        pageSize
    );

    // Получение статистики
    const { data: statsData, loading: loadingStats, refetch: refetchStats } = useMeasurementStatistics(id || null);

    // Общий рефреш для обновления и списка, и статистики
    const handleRefresh = () => {
        refetchMeasurements();
        refetchStats();
    };

    useEffect(() => {
        if (id) {
            vehicleService.getById(id)
                .then(setVehicle)
                .catch(err => toast.error('Ошибка загрузки машины'))
                .finally(() => setLoadingVehicle(false));
        }
    }, [id]);

    if (loadingVehicle) return <SkeletonDetails />;
    if (!vehicle) return <div className="text-center py-10 text-gray-500">Машина не найдена</div>;

    // Определяем статус (активна или выехала)
    const isActive = !vehicle.exitDate;
    const statusColor = isActive ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300' : 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300';
    const statusIcon = isActive ? <Clock className="w-4 h-4" /> : <CheckCircle className="w-4 h-4" />;
    const statusText = isActive ? 'На площадке' : 'Выехал';

    // Форматирование дат
    const formatDate = (dateStr: string) => {
        return new Date(dateStr).toLocaleString('ru-RU', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    // Формируем путь для ссылки «Главная» с сохранением параметров пагинации
    const mainPagePath = searchParams.toString() ? `/?${searchParams.toString()}` : '/';

    return (
        <div>
            {/* Хлебные крошки */}
            <nav className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-4">
                <Link to={mainPagePath} className="hover:text-blue-600 dark:hover:text-blue-400 transition">
                    Главная
                </Link>
                <span>/</span>
                <span className="text-gray-700 dark:text-gray-300 font-medium">{vehicle.number}</span>
            </nav>

            {/* Заголовок с номером и статусом */}
            <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
                <div className="flex items-center gap-3">
                    <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                        <Truck className="w-8 h-8 text-blue-600 dark:text-blue-400" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{vehicle.number}</h1>
                        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                            <span>{vehicle.vehiclePlate}</span>
                            <span className="w-1 h-1 bg-gray-300 dark:bg-gray-600 rounded-full"></span>
                            <span>{vehicle.counterparty}</span>
                        </div>
                    </div>
                </div>
                <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm font-medium ${statusColor}`}>
                    {statusIcon}
                    {statusText}
                </span>
            </div>

            {/* Карточка с информацией о машине */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 p-6 mb-6">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-4">
                    {/* Блок: Основное */}
                    <div className="space-y-2">
                        <h3 className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider flex items-center gap-2">
                            <ClipboardList className="w-4 h-4" /> Основное
                        </h3>
                        <InfoRow icon={<Calendar className="w-4 h-4" />} label="Дата создания пропуска" value={formatDate(vehicle.date)} />
                        <InfoRow icon={<Building2 className="w-4 h-4" />} label="Поставщик" value={vehicle.counterparty} />
                        <InfoRow icon={<FileText className="w-4 h-4" />} label="ИНН поставщика" value={vehicle.inn || '—'} />
                    </div>

                    {/* Блок: Транспорт */}
                    <div className="space-y-2">
                        <h3 className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider flex items-center gap-2">
                            <Car className="w-4 h-4" /> Транспорт
                        </h3>
                        <InfoRow icon={<Gauge className="w-4 h-4" />} label="Марка" value={vehicle.vehicleBrand} />
                        <InfoRow icon={<Car className="w-4 h-4" />} label="Гос. номер" value={vehicle.vehiclePlate} />
                        <InfoRow icon={<Car className="w-4 h-4" />} label="Прицеп" value={vehicle.trailer || '—'} />
                    </div>

                    {/* Блок: Персонал */}
                    <div className="space-y-2">
                        <h3 className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider flex items-center gap-2">
                            <Users className="w-4 h-4" /> Персонал
                        </h3>
                        <InfoRow icon={<User className="w-4 h-4" />} label="Водитель" value={vehicle.driver} />
                    </div>

                    {/* Блок: Даты (можно выделить отдельно) */}
                    <div className="sm:col-span-2 lg:col-span-3 border-t border-gray-100 dark:border-gray-700 pt-4 mt-2 grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <InfoRow icon={<Clock className="w-4 h-4" />} label="Дата въезда" value={formatDate(vehicle.entryDate)} />
                        <InfoRow
                            icon={<Clock className="w-4 h-4" />}
                            label="Дата выезда"
                            value={vehicle.exitDate ? formatDate(vehicle.exitDate) : '—'}
                            valueClassName={!vehicle.exitDate ? 'text-gray-400 dark:text-gray-500' : ''}
                        />
                    </div>

                    {/* ===== НОВЫЙ БЛОК: Разгрузка ===== */}
                    <div className="sm:col-span-2 lg:col-span-3 border-t border-gray-100 dark:border-gray-700 pt-4 mt-2">
                        <h3 className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider flex items-center gap-2 mb-2">
                            <Package className="w-4 h-4" /> Разгрузка
                        </h3>
                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                            <InfoRow
                                icon={<Package className="w-4 h-4" />}
                                label="Количество тюков"
                                value={vehicle.baleCount != null ? String(vehicle.baleCount) : '—'}
                            />
                            <InfoRow
                                icon={<Package className="w-4 h-4" />}
                                label="Порванных тюков"
                                value={vehicle.damagedBaleCount != null ? String(vehicle.damagedBaleCount) : '—'}
                            />
                            <InfoRow
                                icon={<Weight className="w-4 h-4" />}
                                label="Вес (кг)"
                                value={vehicle.weightKg != null ? String(vehicle.weightKg) : '—'}
                            />
                            <InfoRow
                                icon={<Hash className="w-4 h-4" />}
                                label="Номер штабеля"
                                value={vehicle.stackNumber || '—'}
                            />
                        </div>
                    </div>
                </div>
            </div>

            {/* Блок статистики */}
            {loadingStats ? (
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md border border-gray-200 dark:border-gray-700 mb-6">
                    {Array.from({ length: 4 }).map((_, i) => (
                        <div key={i} className="flex items-center gap-3">
                            <div className="animate-pulse bg-gray-200 dark:bg-gray-700 h-10 w-10 rounded-full" />
                            <div>
                                <div className="animate-pulse bg-gray-200 dark:bg-gray-700 h-3 w-24 mb-1 rounded" />
                                <div className="animate-pulse bg-gray-200 dark:bg-gray-700 h-6 w-12 rounded" />
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                statsData && <MeasurementStatistics stats={statsData} />
            )}

            {/* Список замеров */}
            {loadingMeasurements ? (
                <SkeletonMeasurementsList />
            ) : measurementsData ? (
                <MeasurementList
                    vehicleId={vehicle.id}
                    // ИСПРАВЛЕНИЕ: безопасная передача пропсов с резервными значениями
                    measurements={measurementsData?.items ?? []}
                    totalCount={measurementsData?.totalCount ?? 0}
                    pageNumber={measurementsData?.pageNumber ?? 1}
                    pageSize={measurementsData?.pageSize ?? 10}
                    totalPages={measurementsData?.totalPages ?? 0}
                    onPageChange={setPageNumber}
                    onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                    onRefresh={handleRefresh}
                    loading={loadingMeasurements}
                />
            ) : null}
        </div>
    );
}

/**
 * Вспомогательный компонент для отображения одной строки информации.
 */
function InfoRow({ icon, label, value, valueClassName = '' }: { icon: React.ReactNode; label: string; value: string; valueClassName?: string }) {
    return (
        <div className="flex items-start gap-2 text-sm">
            <span className="text-gray-400 dark:text-gray-500 mt-0.5">{icon}</span>
            <div>
                <span className="text-gray-500 dark:text-gray-400">{label}:</span>
                <span className={`ml-1.5 text-gray-800 dark:text-gray-200 font-medium ${valueClassName}`}>{value}</span>
            </div>
        </div>
    );
}