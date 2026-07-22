import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import toast from 'react-hot-toast';
import { vehicleService } from '../services/api';
import { useMeasurements } from '../hooks/useMeasurements';
import MeasurementList from '../components/MeasurementList';
import Spinner from '../components/Spinner';

export default function VehicleDetailsPage() {
    const { id } = useParams<{ id: string }>();
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

    useEffect(() => {
        if (id) {
            vehicleService.getById(id)
                .then(setVehicle)
                .catch(err => toast.error('Ошибка загрузки машины'))
                .finally(() => setLoadingVehicle(false));
        }
    }, [id]);

    if (loadingVehicle) return <Spinner />;
    if (!vehicle) return <div className="text-center py-10 text-gray-500">Машина не найдена</div>;

    return (
        <div>
            <Link to="/" className="inline-flex items-center gap-2 text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 mb-4">
                <ArrowLeft className="w-4 h-4" /> Назад к списку
            </Link>

            <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6 mb-6">
                <h2 className="text-2xl font-bold text-gray-900 dark:text-white">{vehicle.number}</h2>
                <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2 mt-4">
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Контрагент</dt><dd className="text-gray-900 dark:text-white">{vehicle.counterparty}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Вид работ</dt><dd className="text-gray-900 dark:text-white">{vehicle.workType}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Марка</dt><dd className="text-gray-900 dark:text-white">{vehicle.vehicleBrand}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Гос. номер</dt><dd className="text-gray-900 dark:text-white">{vehicle.vehiclePlate}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Прицеп</dt><dd className="text-gray-900 dark:text-white">{vehicle.trailer || '—'}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Водитель</dt><dd className="text-gray-900 dark:text-white">{vehicle.driver}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Грузчик</dt><dd className="text-gray-900 dark:text-white">{vehicle.loader || '—'}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Экспедитор</dt><dd className="text-gray-900 dark:text-white">{vehicle.expeditor || '—'}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Подразделение</dt><dd className="text-gray-900 dark:text-white">{vehicle.department}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Дата въезда</dt><dd className="text-gray-900 dark:text-white">{new Date(vehicle.entryDate).toLocaleString()}</dd></div>
                    <div><dt className="text-sm text-gray-500 dark:text-gray-400">Дата выезда</dt><dd className="text-gray-900 dark:text-white">{vehicle.exitDate ? new Date(vehicle.exitDate).toLocaleString() : '—'}</dd></div>
                </dl>
            </div>

            {measurementsData && (
                <MeasurementList
                    vehicleId={vehicle.id}
                    measurements={measurementsData.items}
                    totalCount={measurementsData.totalCount}
                    pageNumber={measurementsData.pageNumber}
                    pageSize={measurementsData.pageSize}
                    totalPages={measurementsData.totalPages}
                    onPageChange={setPageNumber}
                    onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
                    onRefresh={refetchMeasurements}
                    loading={loadingMeasurements}
                />
            )}
        </div>
    );
}