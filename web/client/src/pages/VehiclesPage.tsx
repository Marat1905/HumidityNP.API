import { useNavigate, useSearchParams } from 'react-router-dom';
import { useVehicles } from '../hooks/useVehicles';
import Pagination from '../components/shared/Pagination';
import { SkeletonTable } from '../components/shared/Skeleton';

export default function VehiclesPage() {
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();

    // Чтение параметров из URL с значениями по умолчанию
    const page = parseInt(searchParams.get('page') || '1', 10);
    const size = parseInt(searchParams.get('size') || '10', 10);

    const { data, loading, error } = useVehicles(page, size);

    // Обработчики изменения пагинации
    const handlePageChange = (newPage: number) => {
        setSearchParams({ page: String(newPage), size: String(size) });
    };

    const handlePageSizeChange = (newSize: number) => {
        setSearchParams({ page: '1', size: String(newSize) });
    };

    if (loading) return <SkeletonTable rows={5} columns={6} />;
    if (error) return <div className="text-red-500 text-center py-10">{error.message}</div>;
    if (!data) return null;

    const { items, totalCount, totalPages } = data;

    // Обработчик перехода на детали с сохранением параметров пагинации
    const handleRowClick = (vehicleId: string) => {
        const queryString = searchParams.toString();
        const path = queryString ? `/vehicles/${vehicleId}?${queryString}` : `/vehicles/${vehicleId}`;
        navigate(path);
    };

    return (
        <div>
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                                Номер пропуска
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                                Поставщик
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                                Гос. номер
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                                Водитель
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                                Замеры
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
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