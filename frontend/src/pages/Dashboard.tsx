import { CastingCard } from '../components/CastingCard';
import { FilterPanel } from '../components/FilterPanel';
import { useFilters } from '../hooks/useFilters';
import { useCastingCalls } from '../hooks/useCastingCalls';
import { castingApi } from '../api/castingApi';
import { useState } from 'react';

export function Dashboard() {
  const { filter, setKeywords, toggleType, toggleRegion, reset } = useFilters();
  const { calls, loading, error, refetch, toggleFavorite, markApplied, unmarkApplied } = useCastingCalls(filter);
  const [scraping, setScraping] = useState(false);

  const handleScrapeAll = async () => {
    setScraping(true);
    try {
      await castingApi.scrapeAll();
      await refetch();
    } finally {
      setScraping(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#0f0f0f] flex flex-col">
      <header className="border-b border-[#2a2a2a] px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="text-2xl">🎬</span>
          <h1 className="text-xl font-bold text-[#f5f5f5]">CastingRadar</h1>
        </div>
        <button
          onClick={handleScrapeAll}
          disabled={scraping}
          className="text-sm px-4 py-2 rounded-lg bg-[#1e1e1e] border border-[#2a2a2a] text-[#9ca3af] hover:border-[#d4af37]/50 hover:text-[#d4af37] transition-colors disabled:opacity-50"
        >
          {scraping ? '⟳ Scraping...' : '⟳ Aggiorna ora'}
        </button>
      </header>

      <div className="flex gap-6 p-6 flex-1">
        <FilterPanel
          keywords={filter.keywords ?? ''}
          selectedTypes={filter.types ?? []}
          selectedRegions={filter.regions ?? []}
          onKeywordsChange={setKeywords}
          onToggleType={toggleType}
          onToggleRegion={toggleRegion}
          onReset={reset}
        />

        <main className="flex-1">
          <div className="flex items-center justify-between mb-4">
            <p className="text-sm text-[#9ca3af]">
              {loading ? 'Caricamento...' : `${calls.length} casting trovati`}
            </p>
          </div>

          {error && (
            <div className="bg-red-900/20 border border-red-800 rounded-lg p-4 text-red-400 text-sm mb-4">
              Errore: {error}
            </div>
          )}

          {loading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {[...Array(6)].map((_, i) => (
                <div key={i} className="bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-4 h-48 animate-pulse" />
              ))}
            </div>
          ) : calls.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-24 text-[#555]">
              <span className="text-5xl mb-4">🎭</span>
              <p className="text-lg">Nessun casting trovato</p>
              <p className="text-sm mt-1">Modifica i filtri o aggiorna le fonti</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {calls.map(c => (
                <CastingCard
                  key={c.id}
                  casting={c}
                  onToggleFavorite={toggleFavorite}
                  onMarkApplied={markApplied}
                  onUnmarkApplied={unmarkApplied}
                />
              ))}
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
