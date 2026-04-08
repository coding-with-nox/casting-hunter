import { useCallback, useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { CastingCall, FilterState } from '../api/types';

const CASTING_CALLS_SYNC_EVENT = 'castingradar:calls-sync';

export function useCastingCalls(filter?: FilterState) {
  const [calls, setCalls] = useState<CastingCall[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const filterKey = JSON.stringify(filter ?? {});

  const fetchCalls = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await castingApi.getCastings(filter);
      setCalls(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [filterKey]);

  useEffect(() => { fetchCalls(); }, [fetchCalls]);

  useEffect(() => {
    const handleSync = () => { void fetchCalls(); };
    window.addEventListener(CASTING_CALLS_SYNC_EVENT, handleSync);
    return () => window.removeEventListener(CASTING_CALLS_SYNC_EVENT, handleSync);
  }, [fetchCalls]);

  const syncCalls = useCallback(() => {
    window.dispatchEvent(new Event(CASTING_CALLS_SYNC_EVENT));
  }, []);

  const toggleFavorite = useCallback(async (id: string) => {
    await castingApi.toggleFavorite(id);
    await fetchCalls();
    syncCalls();
  }, [fetchCalls, syncCalls]);

  const markApplied = useCallback(async (id: string) => {
    await castingApi.markApplied(id);
    await fetchCalls();
    syncCalls();
  }, [fetchCalls, syncCalls]);

  const unmarkApplied = useCallback(async (id: string) => {
    await castingApi.unmarkApplied(id);
    await fetchCalls();
    syncCalls();
  }, [fetchCalls, syncCalls]);

  const toggleHidden = useCallback(async (id: string) => {
    await castingApi.toggleHidden(id);
    await fetchCalls();
    syncCalls();
  }, [fetchCalls, syncCalls]);

  return { calls, loading, error, refetch: fetchCalls, toggleFavorite, markApplied, unmarkApplied, toggleHidden };
}
