import { useCallback, useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { CastingCall, FilterState } from '../api/types';

export function useCastingCalls(filter?: FilterState) {
  const [calls, setCalls] = useState<CastingCall[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
  }, [JSON.stringify(filter)]);

  useEffect(() => { fetchCalls(); }, [fetchCalls]);

  const toggleFavorite = useCallback(async (id: string) => {
    await castingApi.toggleFavorite(id);
    setCalls(prev => prev.map(c => c.id === id ? { ...c, isFavorite: !c.isFavorite } : c));
  }, []);

  const markApplied = useCallback(async (id: string) => {
    await castingApi.markApplied(id);
    setCalls(prev => prev.map(c => c.id === id ? { ...c, isApplied: true } : c));
  }, []);

  const unmarkApplied = useCallback(async (id: string) => {
    await castingApi.unmarkApplied(id);
    setCalls(prev => prev.map(c => c.id === id ? { ...c, isApplied: false } : c));
  }, []);

  return { calls, loading, error, refetch: fetchCalls, toggleFavorite, markApplied, unmarkApplied };
}
