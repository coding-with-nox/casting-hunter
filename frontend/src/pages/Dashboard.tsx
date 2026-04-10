import { useMemo, useState } from 'react';
import { CastingCard } from '../components/CastingCard';
import { FilterPanel } from '../components/FilterPanel';
import { useFilters } from '../hooks/useFilters';
import { useCastingCalls } from '../hooks/useCastingCalls';
import type { CastingCall } from '../api/types';

type SortKey = 'newest' | 'deadline' | 'source' | 'applied_last';

const SORT_OPTIONS: { key: SortKey; label: string }[] = [
  { key: 'newest',      label: 'Più recenti' },
  { key: 'deadline',    label: 'Scadenza' },
  { key: 'source',      label: 'Sorgente' },
  { key: 'applied_last', label: 'Non candidati prima' },
];

function sortCalls(calls: CastingCall[], sort: SortKey): CastingCall[] {
  const copy = [...calls];
  switch (sort) {
    case 'newest':
      return copy.sort((a, b) => new Date(b.scrapedAt).getTime() - new Date(a.scrapedAt).getTime());
    case 'deadline':
      return copy.sort((a, b) => {
        if (!a.deadline && !b.deadline) return 0;
        if (!a.deadline) return 1;
        if (!b.deadline) return -1;
        return new Date(a.deadline).getTime() - new Date(b.deadline).getTime();
      });
    case 'source':
      return copy.sort((a, b) => a.sourceName.localeCompare(b.sourceName));
    case 'applied_last':
      return copy.sort((a, b) => {
        if (a.isApplied === b.isApplied) return new Date(b.scrapedAt).getTime() - new Date(a.scrapedAt).getTime();
        return a.isApplied ? 1 : -1;
      });
  }
}

export function Dashboard() {
  const { filter, setKeywords, toggleType, toggleRegion, toggleShowHidden, reset } = useFilters();
  const { calls, loading, error, toggleFavorite, markApplied, unmarkApplied, toggleHidden } = useCastingCalls(filter);
  const [sort, setSort] = useState<SortKey>('newest');

  const sorted = useMemo(() => sortCalls(calls.filter(c => !c.isFavorite), sort), [calls, sort]);

  return (
    <div className="flex h-full">
      {/* Sidebar filtri */}
      <aside className="w-52 flex-shrink-0 border-r border-[#1a1a1a] p-4 flex flex-col gap-4 overflow-y-auto">
        <FilterPanel
          keywords={filter.keywords ?? ''}
          selectedTypes={filter.types ?? []}
          selectedRegions={filter.regions ?? []}
          showHidden={filter.showHidden ?? false}
          onKeywordsChange={setKeywords}
          onToggleType={toggleType}
          onToggleRegion={toggleRegion}
          onToggleShowHidden={toggleShowHidden}
          onReset={reset}
        />
      </aside>

      {/* Contenuto principale */}
      <main className="flex-1 flex flex-col min-w-0 overflow-y-auto">
        {/* Barra superiore: contatore + ordinamento */}
        <div className="px-5 py-2.5 border-b border-[#1a1a1a] flex items-center justify-between gap-4">
          <p className="text-xs text-[#555]">
            {loading
              ? 'Caricamento...'
              : filter.showHidden
              ? `${sorted.length} nascosti`
              : `${sorted.length} casting`}
          </p>

          <div className="flex items-center gap-2">
            <span className="text-xs text-[#444]">Ordina:</span>
            <select
              value={sort}
              onChange={e => setSort(e.target.value as SortKey)}
              className="bg-[#141414] border border-[#222] rounded-lg px-2 py-1 text-xs text-[#999] focus:outline-none focus:border-[#d4af37]/40 cursor-pointer"
            >
              {SORT_OPTIONS.map(o => (
                <option key={o.key} value={o.key}>{o.label}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="p-5 flex-1">
          {error && (
            <div className="bg-red-900/20 border border-red-800/50 rounded-lg p-3 text-red-400 text-xs mb-4">
              {error}
            </div>
          )}

          {loading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
              {[...Array(8)].map((_, i) => (
                <div key={i} className="bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-4 h-44 animate-pulse" />
              ))}
            </div>
          ) : sorted.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-32 text-[#444]">
              <span className="text-5xl mb-4">🎭</span>
              <p className="text-base font-medium text-[#666]">
                {filter.showHidden ? 'Nessun casting nascosto' : 'Nessun casting trovato'}
              </p>
              <p className="text-sm mt-1">
                {filter.showHidden ? '' : 'Prova ad aggiornare le fonti o cambiare i filtri'}
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
              {sorted.map(c => (
                <CastingCard
                  key={c.id}
                  casting={c}
                  onToggleFavorite={toggleFavorite}
                  onMarkApplied={markApplied}
                  onUnmarkApplied={unmarkApplied}
                  onToggleHidden={toggleHidden}
                />
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
