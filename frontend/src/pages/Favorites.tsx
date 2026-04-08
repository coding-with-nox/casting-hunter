import { CastingCard } from '../components/CastingCard';
import { useCastingCalls } from '../hooks/useCastingCalls';

export function Favorites() {
  const { calls, loading, toggleFavorite, markApplied, unmarkApplied } = useCastingCalls();
  const favorites = calls.filter(c => c.isFavorite);

  if (loading) return <div className="p-8 text-[#9ca3af]">Caricamento...</div>;

  return (
    <div className="p-6">
      <h2 className="text-xl font-bold text-[#f5f5f5] mb-4">⭐ Preferiti ({favorites.length})</h2>
      {favorites.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-24 text-[#555]">
          <span className="text-5xl mb-4">★</span>
          <p className="text-lg">Nessun preferito ancora</p>
          <p className="text-sm mt-1">Clicca ★ su un casting per aggiungerlo</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {favorites.map(c => (
            <CastingCard key={c.id} casting={c} onToggleFavorite={toggleFavorite} onMarkApplied={markApplied} onUnmarkApplied={unmarkApplied} />
          ))}
        </div>
      )}
    </div>
  );
}
