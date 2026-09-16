/**
 * Хук для подписки на SignalR-события, приходящие от Humidity.API.
 * и вызывает переданные колбэки.
 */
import { useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import type {
    VehicleCreatedEvent,
    MeasurementCreatedEvent,
    ShiftEndedEvent,
} from '../types/humidity';

/**
 * Обработчики, которые можно передать в хук. Все — опциональные.
 */
interface SignalREventHandlers {
    /** Создана новая машина. */
    onVehicleCreated?: (evt: VehicleCreatedEvent) => void;

    /** Создан новый замер. */
    onMeasurementCreated?: (evt: MeasurementCreatedEvent) => void;

    /** Обновлена существующая машина (изменены поля, разгрузка, выезд). */
    onVehicleUpdated?: (payload: { vehicleId: string }) => void;

    /** Удалён замер. */
    onMeasurementDeleted?: (payload: { measurementId: string; vehicleId: string }) => void;

    /** Завершена смена (в 08:00 или 20:00 локального времени площадки). */
    onShiftEnded?: (evt: ShiftEndedEvent) => void;
}

/**
 * Хук подписки на SignalR-события.
 *
 * @param handlers объект с колбэками на нужные события.
 */
export const useSignalREvents = (handlers: SignalREventHandlers) => {
    const { signalR } = useAuth();

    // Ref хранит актуальный объект handlers. Это позволяет колбэкам,
    // зарегистрированным один раз, вызывать последнюю версию функций
    // без пересоздания подписок при каждом рендере.
    const handlersRef = useRef(handlers);
    handlersRef.current = handlers;

    useEffect(() => {
        if (!signalR) return;

        const unsubscribers: Array<() => void> = [];

        if (handlersRef.current.onVehicleCreated) {
            unsubscribers.push(
                signalR.onVehicleCreated(evt =>
                    handlersRef.current.onVehicleCreated?.(evt)
                )
            );
        }

        if (handlersRef.current.onMeasurementCreated) {
            unsubscribers.push(
                signalR.onMeasurementCreated(evt =>
                    handlersRef.current.onMeasurementCreated?.(evt)
                )
            );
        }

        if (handlersRef.current.onVehicleUpdated) {
            unsubscribers.push(
                signalR.onVehicleUpdated(payload =>
                    handlersRef.current.onVehicleUpdated?.(payload)
                )
            );
        }

        if (handlersRef.current.onMeasurementDeleted) {
            unsubscribers.push(
                signalR.onMeasurementDeleted(payload =>
                    handlersRef.current.onMeasurementDeleted?.(payload)
                )
            );
        }

        if (handlersRef.current.onShiftEnded) {
            unsubscribers.push(
                signalR.onShiftEnded(evt =>
                    handlersRef.current.onShiftEnded?.(evt)
                )
            );
        }

        // Cleanup: при размонтировании компонента или при смене signalR-инстанса
        // (что маловероятно) — отписываемся от всех событий.
        return () => {
            unsubscribers.forEach(fn => fn());
        };
    }, [signalR]);
};