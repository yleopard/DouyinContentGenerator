import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;

export const getConnection = () => connection;

export const startConnection = async (): Promise<signalR.HubConnection> => {
  if (connection?.state === 'Connected') return connection;

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/generation', {
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Information)
    .build();

  connection.onreconnected(async (connectionId) => {
    console.log('SignalR reconnected:', connectionId);
    await connection?.invoke('ResubscribeToTasks').catch(console.warn);
  });

  connection.onclose(() => {
    console.log('SignalR connection closed');
  });

  await connection.start();
  console.log('SignalR connected');
  return connection;
};

export const stopConnection = async () => {
  if (connection) {
    await connection.stop();
    connection = null;
  }
};

export const subscribeToTask = async (taskId: string) => {
  await connection?.invoke('SubscribeToTask', taskId);
};

export const unsubscribeFromTask = async (taskId: string) => {
  await connection?.invoke('UnsubscribeFromTask', taskId);
};

export const onTaskProgress = (callback: (data: { taskId: string; progress: number; status: string; message: string }) => void) => {
  connection?.on('TaskProgressUpdated', callback);
  return () => connection?.off('TaskProgressUpdated', callback);
};

export interface ItemStatus {
  taskId: string;
  name: string;
  type: 'image' | 'text';
  status: 'calling' | 'success' | 'failed' | 'cancelled';
  detail: string;
  time: string;
}

export const onItemStatus = (callback: (data: ItemStatus) => void) => {
  connection?.on('ItemStatusUpdated', callback);
  return () => connection?.off('ItemStatusUpdated', callback);
};
