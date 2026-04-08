import type { CastingCall } from '../api/types';
import { SourceBadge } from './SourceBadge';

interface Props {
  casting: CastingCall;
  onToggleFavorite: (id: string) => void;
  onMarkApplied: (id: string) => void;
  onUnmarkApplied: (id: string) => void;
}

export function CastingCard({ casting, onToggleFavorite, onMarkApplied, onUnmarkApplied }: Props) {
  const deadline = casting.deadline
    ? new Date(casting.deadline).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' })
    : null;

  return (
    <div className="bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-4 flex flex-col gap-3 hover:border-[#d4af37]/30 transition-colors">
      <div className="flex items-start justify-between gap-2">
        <h3 className="text-[#f5f5f5] font-semibold text-base leading-snug flex-1">
          {casting.title}
        </h3>
        <button
          onClick={() => onToggleFavorite(casting.id)}
          className={`text-xl flex-shrink-0 transition-transform hover:scale-110 ${casting.isFavorite ? 'text-[#d4af37]' : 'text-[#555]'}`}
          title={casting.isFavorite ? 'Rimuovi dai preferiti' : 'Aggiungi ai preferiti'}
        >
          ★
        </button>
      </div>

      <div className="flex flex-wrap gap-2 items-center">
        <SourceBadge name={casting.sourceName} region={casting.region} />
        <span className="text-xs px-2 py-0.5 rounded-full bg-[#2a2a2a] text-[#9ca3af]">{casting.type}</span>
        {casting.isPaid && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-yellow-900/50 text-yellow-400 font-medium">
            💰 Retribuito
          </span>
        )}
        {casting.isApplied && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-900/50 text-emerald-400">
            ✓ Candidata
          </span>
        )}
      </div>

      {casting.location && (
        <p className="text-sm text-[#9ca3af]">📍 {casting.location}</p>
      )}

      {deadline && (
        <p className="text-sm text-[#9ca3af]">⏰ Scadenza: <span className="text-[#d4af37]">{deadline}</span></p>
      )}

      {casting.description && (
        <p className="text-sm text-[#9ca3af] line-clamp-3">{casting.description}</p>
      )}

      <div className="flex gap-2 mt-auto pt-2 border-t border-[#2a2a2a]">
        <a
          href={casting.sourceUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="flex-1 text-center text-sm py-1.5 px-3 rounded-lg bg-[#d4af37] text-black font-medium hover:bg-[#b8962c] transition-colors"
        >
          Vai alla candidatura →
        </a>
        {casting.isApplied ? (
          <button
            onClick={() => onUnmarkApplied(casting.id)}
            className="text-sm py-1.5 px-3 rounded-lg border border-[#2a2a2a] text-[#9ca3af] hover:border-red-500/50 hover:text-red-400 transition-colors"
          >
            Annulla candidatura
          </button>
        ) : (
          <button
            onClick={() => onMarkApplied(casting.id)}
            className="text-sm py-1.5 px-3 rounded-lg border border-[#2a2a2a] text-[#9ca3af] hover:border-[#d4af37]/50 hover:text-[#d4af37] transition-colors"
          >
            Conferma candidatura
          </button>
        )}
      </div>
    </div>
  );
}
