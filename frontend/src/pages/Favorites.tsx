import { CastingCard } from '../components/CastingCard';
import { useCastingCalls } from '../hooks/useCastingCalls';

export function Favorites() {
  const { calls, loading, toggleFavorite, markApplied, unmarkApplied, toggleHidden } = useCastingCalls();
  const favorites = calls.filter(c => c.isFavorite);

  if (loading) return <div className="p-8 text-[#666]">Caricamento...</div>;

  return (
    <div className="p-5">
      <p className="text-sm text-[#555] mb-4">{favorites.length} casting nei preferiti</p>
      {favorites.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-32 text-[#444]">
          <span className="text-5xl mb-4">★</span>
          <p className="text-base font-medium text-[#666]">Nessun preferito ancora</p>
          <p className="text-sm mt-1">Clicca ★ su un casting per salvarlo qui</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
          {favorites.map(c => (
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
  );
}
