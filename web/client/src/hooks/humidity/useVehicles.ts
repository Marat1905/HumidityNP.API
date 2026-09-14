import { useState, useEffect, useCallback, useRef } from 'react';
import { vehicleService } from '../../services/humidity/api';
import type { VehicleDto, PagedResult, VehiclesQueryParams } from '../../types/humidity';

/**
 * Параметры для ручного/тихого обновления данных.
 */
interface FetchOptions {
    /**
     * silent = true — не выставлять loading=true, не показывать скелетон.
     * Используется для автообновления, чтобы не сбрасывать фокус и позицию скролла.
     * При этом isRefreshing всё равно станет true — спиннер на кнопке будет крутиться.
     */
    silent?: boolean;

    /**
     * force = true — игнорировать кэш по params и выполнить запрос принудительно.
     * Используется для ручного и тихого обновления, потому что параметры
     * при этом не меняются, и обычная проверка «params не изменились → пропускаем»
     * заблокировала бы перезапрос.
     */
    force?: boolean;
}

/**
 * Хук для загрузки списка машин с пагинацией, фильтрами и поддержкой
 * тихого/принудительного обновления.
 *
 * Разделение двух флагов:
 *  - loading — «идёт первая загрузка, покажи скелетон». Ставится только при
 *    обычной загрузке (без silent). Именно от него зависит показ SkeletonTable.
 *  - isRefreshing — «идёт любой запрос, покажи спиннер на кнопке Обновить».
 *    Ставится всегда, независимо от silent/force.
 *
 * Это даёт возможность:
 *  - при автообновлении крутить спиннер, но не мигать скелетоном;
 *  - при ручном обновлении крутить спиннер;
 *  - при первой загрузке показать скелетон.
 */
export const useVehicles = (params: VehiclesQueryParams) => {
    const [data, setData] = useState<PagedResult<VehicleDto> | null>(null);
    const [loading, setLoading] = useState(false);
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    // Ref для сравнения params между вызовами: если params не изменились,
    // обычный вызов fetchData не будет повторять запрос.
    const prevParamsRef = useRef<string>('');

    const fetchData = useCallback(
        async (options?: FetchOptions) => {
            const silent = options?.silent ?? false;
            const force = options?.force ?? false;

            const paramsKey = JSON.stringify(params);

            // Обычный вызов: если params не изменились — не делаем запрос.
            // Принудительный вызов (force=true) — всегда делаем запрос.
            if (!force && prevParamsRef.current === paramsKey) {
                return;
            }
            prevParamsRef.current = paramsKey;

            // В silent-режиме НЕ выставляем loading — скелетон не появится,
            // таблица не размонтируется, фокус в фильтрах и позиция скролла сохранятся.
            if (!silent) setLoading(true);
            // Спиннер на кнопке показываем при любом запросе — это визуальный индикатор,
            // что приложение работает и данные обновляются.
            setIsRefreshing(true);
            setError(null);

            try {
                const result = await vehicleService.getAll(params);
                setData(result);
            } catch (err: any) {
                setError(
                    err instanceof Error
                        ? err
                        : new Error(err?.response?.data?.message || 'Ошибка загрузки данных')
                );
            } finally {
                if (!silent) setLoading(false);
                setIsRefreshing(false);
            }
        },
        [params]
    );

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    // Ручное обновление: принудительно + со спиннером.
    const refetch = useCallback(() => fetchData({ force: true }), [fetchData]);

    // Тихий рефетч: принудительно + без скелетона, но со спиннером.
    // Для автообновления.
    const silentRefetch = useCallback(
        () => fetchData({ silent: true, force: true }),
        [fetchData]
    );

    return { data, loading, isRefreshing, error, refetch, silentRefetch };
};