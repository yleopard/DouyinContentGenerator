import { useEffect, useState } from 'react';
import {
  startConnection, subscribeToTask, unsubscribeFromTask,
  onTaskProgress, onItemStatus,
} from '../services/signalr';
import type { TaskProgressUpdate } from '../types';

export interface ItemStatus {
  taskId: string;
  name: string;
  type: 'image' | 'text';
  status: 'calling' | 'success' | 'failed' | 'cancelled';
  detail: string;
  time: string;
}

export const useSignalR = (taskId?: string, onProgress?: (data: TaskProgressUpdate) => void) => {
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    if (!taskId) { setConnected(false); return; }
    let cleanup: (() => void) | undefined;
    let cancelled = false;

    startConnection()
      .then((conn) => {
        if (cancelled) return;
        setConnected(true);
        subscribeToTask(taskId);
        cleanup = onTaskProgress((data) => onProgress?.(data as TaskProgressUpdate));
      })
      .catch(console.warn);

    return () => {
      cancelled = true;
      if (taskId) unsubscribeFromTask(taskId).catch(() => {});
      cleanup?.();
    };
  }, [taskId]);

  return { connected };
};

export const useItemStatus = (taskId?: string, onItem?: (data: ItemStatus) => void) => {
  useEffect(() => {
    if (!taskId) return;
    const cleanup = onItemStatus((data) => {
      if (data.taskId === taskId) onItem?.(data);
    });
    return () => cleanup?.();
  }, [taskId]);
};
