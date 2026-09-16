/**
 * SignalR-клиент для получения real-time уведомлений от Humidity.API.
 */
import * as signalR from '@microsoft/signalr';
import type {
    VehicleCreatedEvent,
    MeasurementCreatedEvent,
    ShiftEndedEvent,
} from '../types/humidity';

/**
 * Базовый URL хаба. В dev-режиме Vite проксирует /hubs → localhost:8090.
 * В production-сборке — через YARP-шлюз.
 */
const HUB_URL = import.meta.env.VITE_SIGNALR_URL ?? '/hubs/humidity';

export type VehicleCreatedHandler = (evt: VehicleCreatedEvent) => void;
export type MeasurementCreatedHandler = (evt: MeasurementCreatedEvent) => void;
export type VehicleUpdatedHandler = (payload: { vehicleId: string }) => void;
export type MeasurementDeletedHandler = (payload: { measurementId: string; vehicleId: string }) => void;
export type ShiftEndedHandler = (evt: ShiftEndedEvent) => void;

/**
 * Обёртка над SignalR HubConnection.
 */
class HumiditySignalRClient {
    private connection: signalR.HubConnection | null = null;
    private accessTokenFactory: () => Promise<string>;
    private isStarting = false;
    private stopped = false;

    constructor(accessTokenFactory: () => Promise<string>) {
        this.accessTokenFactory = accessTokenFactory;
    }

    /**
     * Установить соединение. Метод идемпотентен.
     */
    async start(): Promise<void> {
        if (this.stopped) return;
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            return;
        }
        if (this.isStarting) {
            return;
        }

        this.isStarting = true;

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(HUB_URL, {
                    accessTokenFactory: async () => {
                        try {
                            return await this.accessTokenFactory();
                        } catch {
                            return '';
                        }
                    },
                    transport: signalR.HttpTransportType.WebSockets
                        | signalR.HttpTransportType.LongPolling,
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (ctx) => {
                        // Экспоненциальный backoff: 0с, 2с, 5с, 10с, 30с
                        const delays = [0, 2000, 5000, 10000, 30000];
                        return delays[Math.min(ctx.previousRetryCount, delays.length - 1)];
                    },
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Автоматически подписываемся на каналы после каждого переподключения.
            this.connection.onreconnected(async () => {
                await this.subscribeDefaultChannels();
            });

            await this.connection.start();

            if (this.stopped) {
                await this.connection.stop();
                return;
            }

            await this.subscribeDefaultChannels();
        } finally {
            this.isStarting = false;
        }
    }

    /**
     * Подписка на каналы по умолчанию.
     */
    private async subscribeDefaultChannels(): Promise<void> {
        if (!this.connection) return;
        try {
            await this.connection.invoke(
                'SubscribeToChannel',
                ['vehicles', 'measurements', 'shift']
            );
        } catch (err) {
            console.warn('SignalR: не удалось подписаться на каналы', err);
        }
    }

    /**
     * Остановить соединение. Метод идемпотентен.
     */
    async stop(): Promise<void> {
        this.stopped = true;
        if (!this.connection) return;

        try {
            await this.connection.stop();
        } finally {
            this.connection = null;
        }
    }

    onVehicleCreated(handler: VehicleCreatedHandler): () => void {
        this.connection?.on('vehicleCreated', handler);
        return () => this.connection?.off('vehicleCreated', handler);
    }

    onMeasurementCreated(handler: MeasurementCreatedHandler): () => void {
        this.connection?.on('measurementCreated', handler);
        return () => this.connection?.off('measurementCreated', handler);
    }

    onVehicleUpdated(handler: VehicleUpdatedHandler): () => void {
        this.connection?.on('vehicleUpdated', handler);
        return () => this.connection?.off('vehicleUpdated', handler);
    }

    onMeasurementDeleted(handler: MeasurementDeletedHandler): () => void {
        this.connection?.on('measurementDeleted', handler);
        return () => this.connection?.off('measurementDeleted', handler);
    }

    onShiftEnded(handler: ShiftEndedHandler): () => void {
        this.connection?.on('shiftEnded', handler);
        return () => this.connection?.off('shiftEnded', handler);
    }
}

/**
 * Фабрика клиента.
 */
export const createSignalRClient = (accessTokenFactory: () => Promise<string>) =>
    new HumiditySignalRClient(accessTokenFactory);

export type { HumiditySignalRClient };