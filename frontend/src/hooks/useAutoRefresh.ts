import { useCallback, useEffect, useRef, useState } from 'react';

const STORAGE_KEY = 'castingradar_refresh_interval';
export const DEFAULT_INTERVAL = 60;

export function getStoredInterval(): number {
  const raw = localStorage.getItem(STORAGE_KEY);
  const n = raw ? parseInt(raw) : DEFAULT_INTERVAL;
  return isNaN(n) || n < 10 ? DEFAULT_INTERVAL : n;
}

export function setStoredInterval(seconds: number) {
  localStorage.setItem(STORAGE_KEY, String(seconds));
  // Dispatch manually so the hook in the same tab can react
  window.dispatchEvent(new StorageEvent('storage', { key: STORAGE_KEY, newValue: String(seconds) }));
}

export function useAutoRefresh(onRefresh: () => Promise<void>) {
  const [interval, setIntervalSecs] = useState<number>(getStoredInterval);
  const [secondsLeft, setSecondsLeft] = useState<number>(getStoredInterval);
  const [refreshing, setRefreshing] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const onRefreshRef = useRef(onRefresh);
  onRefreshRef.current = onRefresh;

  const reset = useCallback((newInterval?: number) => {
    const secs = newInterval ?? interval;
    setSecondsLeft(secs);
  }, [interval]);

  const triggerNow = useCallback(async () => {
    if (refreshing) return;
    setRefreshing(true);
    try {
      await onRefreshRef.current();
    } finally {
      setRefreshing(false);
      setSecondsLeft(interval);
    }
  }, [refreshing, interval]);

  const changeInterval = useCallback((seconds: number) => {
    setStoredInterval(seconds);
    setIntervalSecs(seconds);
    setSecondsLeft(seconds);
  }, []);

  // Reload interval when settings page writes to localStorage
  useEffect(() => {
    const handler = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        const n = parseInt(e.newValue);
        if (!isNaN(n) && n >= 10) changeInterval(n);
      }
    };
    window.addEventListener('storage', handler);
    return () => window.removeEventListener('storage', handler);
  }, [changeInterval]);

  useEffect(() => {
    if (timerRef.current) clearInterval(timerRef.current);
    timerRef.current = setInterval(() => {
      setSecondsLeft(prev => {
        if (prev <= 1) {
          setRefreshing(true);
          onRefreshRef.current().finally(() => {
            setRefreshing(false);
            setSecondsLeft(interval);
          });
          return interval;
        }
        return prev - 1;
      });
    }, 1000);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [interval]);

  return { secondsLeft, interval, refreshing, triggerNow, changeInterval, reset };
}
