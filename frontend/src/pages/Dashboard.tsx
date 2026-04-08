import { CastingCard } from '../components/CastingCard';
import { FilterPanel } from '../components/FilterPanel';
import { useFilters } from '../hooks/useFilters';
import { useCastingCalls } from '../hooks/useCastingCalls';

export function Dashboard() {
  const { filter, setKeywords, toggleType, toggleRegion, toggleShowHidden, reset } = useFilters();
  const { calls, loading, error, toggleFavorite, markApplied, unmarkApplied, toggleHidden } = useCastingCalls(filter);

  const visibleCount = calls.length;

  return (
    <div className="flex h-full">
      {/* Sidebar filtri */}
      <aside className="w-56 flex-shrink-0 border-r border-[#1e1e1e] p-4 flex flex-col gap-4 overflow-y-auto">
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
        <div className="px-5 py-3 border-b border-[#1e1e1e] flex items-center justify-between">
          <p className="text-sm text-[#555]">
            {loading
              ? 'Caricamento...'
              : filter.showHidden
              ? `${visibleCount} casting nascosti`
              : `${visibleCount} casting disponibili`}
          </p>
        </div>

        <div className="p-5 flex-1">
          {error && (
            <div className="bg-red-900/20 border border-red-800/50 rounded-lg p-3 text-red-400 text-sm mb-4">
              {error}
            </div>
          )}

          {loading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
              {[...Array(8)].map((_, i) => (
                <div key={i} className="bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-4 h-44 animate-pulse" />
              ))}
            </div>
          ) : calls.length === 0 ? (
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
              {calls.map(c => (
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
