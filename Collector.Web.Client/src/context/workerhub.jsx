import React, { useContext, useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { apiBasePath } from '@/helpers/endpoints';
import { useSession } from '@/context/session';

const WorkerHubContext = React.createContext({
    call: async () => { throw new Error('WorkerHubProvider not mounted'); }
});

const WorkerHubProvider = ({ children }) => {
    const session = useSession();
    const [isConnecting, setIsConnecting] = useState(false);
    const connectionRef = useRef(null);
    const handlersRef = useRef(new Map()); // workerId -> [handler]

    const ensureConnection = useCallback(async () => {
        if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
            return connectionRef.current;
        }
        if (isConnecting) {
            return new Promise((resolve, reject) => {
                const check = () => {
                    if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
                        resolve(connectionRef.current);
                    } else if (!isConnecting) {
                        reject(new Error('Failed to connect to WorkerHub'));
                    } else {
                        setTimeout(check, 100);
                    }
                };
                check();
            });
        }

        setIsConnecting(true);
        try {
            const conn = new signalR.HubConnectionBuilder()
                .withUrl(apiBasePath() + '/worker', {
                    withCredentials: true,
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 1000, 5000, 10000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup global WorkerProgress listener once
            conn.on('WorkerProgress', (appUserId, workerId, eventName, payload) => {
                const currentAppUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
                // Compare as strings to handle number vs string mismatch
                if (!currentAppUserId || String(currentAppUserId) !== String(appUserId)) {
                    return;
                }

                const key = workerId?.toString();
                if (!key) return;

                const handlers = handlersRef.current.get(key);
                if (!handlers || handlers.length === 0) return;

                handlers.forEach((handler) => {
                    try {
                        handler({ eventName, payload, workerId: key });
                    } catch (err) {
                        console.error('WorkerHub handler error:', err);
                    }
                });

                // Auto-unsubscribe when complete or error
                if (eventName === 'DownloadComplete' || eventName === 'DownloadError') {
                    handlersRef.current.delete(key);
                }
            });

            await conn.start();
            connectionRef.current = conn;

            // After connecting, request progress updates for all existing workers
            const currentAppUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
            if (currentAppUserId) {
                try {
                    await conn.invoke('ProgressAll', currentAppUserId);
                } catch (err) {
                    console.error('Error invoking ProgressAll on WorkerHub:', err);
                }
            }

            return conn;
        } finally {
            setIsConnecting(false);
        }
    }, [isConnecting]);

    useEffect(() => {
        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        };
    }, []);

    const call = useCallback(async (route, method, args, onMessage, customId, url) => {
        const appUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
        if (!appUserId) {
            throw new Error('No AppUserId available in session');
        }

        const conn = await ensureConnection();
        const workerId = await conn.invoke('Call', appUserId, route, method, args || null, customId || null, url || null);

        if (onMessage && workerId) {
            const key = workerId.toString();
            const existing = handlersRef.current.get(key) || [];
            existing.push(onMessage);
            handlersRef.current.set(key, existing);
        }
        return workerId;
    }, [ensureConnection, session]);

    const getWorkers = useCallback(async () => {
        const appUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
        if (!appUserId) {
            throw new Error('No AppUserId available in session');
        }

        const conn = await ensureConnection();
        const workers = await conn.invoke('GetWorkersForUser', appUserId);
        return workers || [];
    }, [ensureConnection, session]);

    const subscribe = useCallback(async (workerId, onMessage) => {
        if (!workerId || !onMessage) return;
        await ensureConnection();
        const key = workerId.toString();
        const existing = handlersRef.current.get(key) || [];
        existing.push(onMessage);
        handlersRef.current.set(key, existing);

        return () => {
            const current = handlersRef.current.get(key) || [];
            handlersRef.current.set(
                key,
                current.filter((h) => h !== onMessage)
            );
        };
    }, [ensureConnection]);

    const requestProgress = useCallback(async (workerId) => {
        const appUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
        if (!appUserId || !workerId) return;

        const conn = await ensureConnection();
        await conn.invoke('RequestProgress', appUserId, workerId);
    }, [ensureConnection, session]);

    const progressAll = useCallback(async () => {
        const appUserId = session?.user?.appUserId || session?.user?.AppUser || session?.user?.id || session?.user?.userId;
        if (!appUserId) return;

        const conn = await ensureConnection();
        await conn.invoke('ProgressAll', appUserId);
    }, [ensureConnection, session]);

    const value = {
        call,
        getWorkers,
        subscribe,
        requestProgress,
        progressAll
    };

    return (
        <WorkerHubContext.Provider value={value}>
            {children}
        </WorkerHubContext.Provider>
    );
};

const useWorkerHub = () => {
    const context = useContext(WorkerHubContext);
    if (!context) {
        throw new Error('useWorkerHub must be used within WorkerHubProvider');
    }
    return context;
};

export {
    WorkerHubProvider,
    useWorkerHub
};
