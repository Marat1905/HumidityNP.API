import React, { useState } from 'react';
import { useSupplierDetails, useSupplierChartData } from '../../hooks/humidity';
import { SkeletonReport } from '../common';
import { SupplierChart, SupplierVehiclesTable, MeasurementStatistics } from '../humidity';

interface SupplierDetailsProps {
    supplierInn: string;
    fromDate?: Date;
    toDate?: Date;
}

/**
 * Компонент детальной информации по поставщику.
 *
 * Содержит три независимых по жизненному циклу блока:
 *
 *  1) Общая статистика по всем машинам поставщика за период.
 *     Приходит вместе с ответом таблицы, но не зависит от текущей страницы
 *     (сервер считает её по полному набору машин).
 *
 *  2) График диапазона влажности по всем машинам за период.
 *     Загружается ОДИН раз при изменении периода/поставщика и НЕ перезапрашивается
 *     при смене страницы пагинации таблицы. Состояние хука useSupplierChartData
 *     сохраняется между ре-рендерами родителя, так как pageNumber не входит
 *     в его зависимости.
 *
 *  3) Таблица машин с серверной пагинацией.
 *     Перезапрашивает данные при смене страницы или размера страницы.
 *
 * КЛЮЧЕВОЙ МОМЕНТ: раньше компонент делал ранний возврат `if (loading) return <SkeletonReport />;`
 * на КАЖДУЮ загрузку, в том числе на смену страницы пагинации. Это размонтировало
 * <SupplierChart />, и Recharts заново проигрывал анимации — визуально «график перерисовывался».
 * Теперь ранний возврат делается только при ПЕРВОЙ загрузке, когда ещё нет ни данных
 * таблицы, ни данных графика.
 */
const SupplierDetails: React.FC<SupplierDetailsProps> = ({ supplierInn, fromDate, toDate }) => {
    // Пагинация таблицы машин — на стороне сервера.
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    // Сортировка по дате въезда (новые сверху) — выполнена на сервере.
    const sortOrder: 'asc' | 'desc' = 'desc';

    // === График: полный (без пагинации) список машин, отсортированный сервером ===
    // Загружается один раз при смене периода/поставщика.
    // Зависимости хука: [inn, fromDate, toDate, order].
    // pageNumber в зависимости НЕ входит, поэтому смена страницы таблицы
    // не вызывает повторный запрос и не сбрасывает состояние.
    const {
        data: chartData,
        loading: chartLoading,
        error: chartError,
    } = useSupplierChartData(supplierInn, fromDate || null, toDate || null, sortOrder);

    // === Таблица: постраничный список машин ===
    // Загружается заново при смене страницы или размера страницы.
    // Хук сохраняет предыдущее значение data до прихода нового ответа
    // (stale-while-revalidate), поэтому таблица не «мигает» скелетоном.
    const {
        data,
        loading: detailsLoading,
        error: detailsError,
    } = useSupplierDetails(
        supplierInn,
        fromDate || null,
        toDate || null,
        pageNumber,
        pageSize,
        sortOrder
    );

    // Полный скелетон показываем ТОЛЬКО при самой первой загрузке,
    // когда ещё нет ни данных таблицы, ни данных графика.
    // Это предотвращает размонтирование графика при смене страницы пагинации.
    const isInitialLoading =
        !data && chartData.length === 0 && (detailsLoading || chartLoading);

    if (isInitialLoading) return <SkeletonReport />;

    if (detailsError) {
        return (
            <div className="text-red-500 text-center py-4">
                {detailsError.message}
            </div>
        );
    }

    if (!data) {
        return (
            <div className="text-center py-4 text-gray-500 dark:text-gray-400">
                Нет данных по этому поставщику за выбранный период
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Общая статистика по всем машинам за период — не зависит от страницы. */}
            <MeasurementStatistics stats={data.overallStatistics} />

            {/* График: после первой загрузки всегда смонтирован.
                При смене страницы пагинации данные (chartData) не меняются,
                React.memo внутри SupplierChart предотвращает лишний ре-рендер,
                а сам компонент не размонтируется, поэтому Recharts не проигрывает
                анимации заново. */}
            {chartLoading && chartData.length === 0 ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
                    Загрузка графика...
                </div>
            ) : chartError ? (
                <div className="text-red-500 text-center py-4">
                    {chartError.message}
                </div>
            ) : (
                <SupplierChart vehicles={chartData} />
            )}

            {/* Таблица машин с серверной пагинацией.
                При смене страницы родитель ре-рендерится, хук useSupplierDetails
                загружает новые данные. Компонент таблицы показывает старые данные
                до прихода новых и лёгкий индикатор загрузки (isLoading). */}
            <SupplierVehiclesTable
                vehicles={data.vehicles}
                onPageChange={setPageNumber}
                onPageSizeChange={(size) => {
                    setPageSize(size);
                    setPageNumber(1);
                }}
                isLoading={detailsLoading}
            />
        </div>
    );
};

export default SupplierDetails;