import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useVehicles } from '../hooks/useVehicles';
import Pagination from '../components/Pagination';
import Spinner from '../components/Spinner';

export default function VehiclesPage() {
    const navigate = useNavigate();
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const { data, loading, error } = useVehicles(pageNumber, pageSize);

    if (loading) return <Spinner />;
    if (error) return <div className="text-red-500 text-center py-10">{error}</div>;
    if (!data) return null;

    const { items, totalCount, totalPages } = data;

    return (
        <div>
            {/* Заголовок удалён, так как он есть в MainPage */}
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-800">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Заявка</th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Контрагент</th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Гос. номер</th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Водитель</th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Замеры</th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Статус</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        {items.map((vehicle) => (
                            <tr
                                key={vehicle.id}
                                onClick={() => navigate(`/vehicles/${vehicle.id}`)}
                                className="hover:bg-gray-50 dark:hover:bg-gray-800 transition cursor-pointer"
                                role="button"
                                tabIndex={0}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' || e.key === ' ') {
                                        e.preventDefault();
                                        navigate(`/vehicles/${vehicle.id}`);
                                    }
                                }}
                            >
                                <td className="px-4 py-3 text-sm text-gray-900 dark:text-white">{vehicle.number}</td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{vehicle.counterparty}</td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{vehicle.vehiclePlate}</td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{vehicle.driver}</td>
                                <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 text-center">
                                    {vehicle.measurementsCount}
                                </td>
                                <td className="px-4 py-3 text-sm">
                                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${vehicle.exitDate
                                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
                                        : 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
                                        }`}>
                                        {vehicle.exitDate ? 'Выехал' : 'На площадке'}
                                    </span>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                onPageChange={(page) => setPageNumber(page)}
                pageSize={pageSize}
                onPageSizeChange={(size) => {
                    setPageSize(size);
                    setPageNumber(1);
                }}
                totalCount={totalCount}
            />
        </div>
    );
}