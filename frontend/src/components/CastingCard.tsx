import { useState } from 'react';
import type { CastingCall } from '../api/types';
import { SourceBadge } from './SourceBadge';

interface Props {
  casting: CastingCall;
  onToggleFavorite: (id: string) => void;
  onMarkApplied: (id: string) => void;
  onUnmarkApplied: (id: string) => void;
  onToggleHidden: (id: string) => void;
}

export function CastingCard({ casting, onToggleFavorite, onMarkApplied, onUnmarkApplied, onToggleHidden }: Props) {
  const [copied, setCopied] = useState(false);

  const handleCopyLink = () => {
    navigator.clipboard.writeText(casting.sourceUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const deadline = casting.deadline
    ? new Date(casting.deadline).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' })
    : null;

  const isExpired = casting.deadline ? new Date(casting.deadline) < new Date() : false;

  return (
    <div className={`bg-[#1e1e1e] border rounded-xl p-4 flex flex-col gap-3 transition-all ${
      casting.isApplied
        ? 'border-emerald-800/60 hover:border-emerald-700/60'
        : 'border-[#2a2a2a] hover:border-[#d4af37]/30'
    }`}>
      {/* Header: titolo + azioni */}
      <div className="flex items-start gap-2">
        <h3 className="text-[#f5f5f5] font-semibold text-sm leading-snug flex-1">
          {casting.title}
        </h3>
        <div className="flex items-center gap-1 flex-shrink-0">
          {/* Preferiti */}
          <button
            onClick={() => onToggleFavorite(casting.id)}
            title={casting.isFavorite ? 'Rimuovi dai preferiti' : 'Aggiungi ai preferiti'}
            aria-label={casting.isFavorite ? 'Rimuovi dai preferiti' : 'Aggiungi ai preferiti'}
            className={`w-7 h-7 flex items-center justify-center rounded-lg text-[0px] transition-colors hover:bg-[#2a2a2a] hover:text-[#d4af37] ${
              casting.isFavorite ? 'text-[#d4af37]' : 'text-[#444]'
            }`}
          >
            <svg
              aria-hidden="true"
              viewBox="0 0 24 24"
              className="w-4 h-4"
              fill={casting.isFavorite ? 'currentColor' : 'none'}
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M12 3.5l2.63 5.33 5.87.85-4.25 4.14 1 5.84L12 17l-5.25 2.66 1-5.84-4.25-4.14 5.87-.85L12 3.5z" />
            </svg>
            ★
          </button>
          <button
            onClick={() => onToggleHidden(casting.id)}
            title="Scarta casting"
            aria-label="Scarta casting"
            className="w-7 h-7 flex items-center justify-center rounded-lg text-[#444] hover:bg-[#2a2a2a] hover:text-red-400 transition-colors"
          >
            <svg
              aria-hidden="true"
              viewBox="0 0 24 24"
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="12" cy="12" r="8" />
              <path d="M8.5 8.5l7 7" />
            </svg>
          </button>
        </div>
      </div>

      {/* Badge: fonte, tipo, stato */}
      <div className="flex flex-wrap gap-1.5 items-center">
        <SourceBadge name={casting.sourceName} region={casting.region} />
        <span className="text-xs px-2 py-0.5 rounded-full bg-[#252525] text-[#777]">{casting.type}</span>
        {casting.isPaid && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-yellow-900/40 text-yellow-400 font-medium">
            💰 Pagato
          </span>
        )}
        {casting.isApplied && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-900/40 text-emerald-400 font-medium">
            ✓ Candidata
          </span>
        )}
      </div>

      {/* Info */}
      <div className="flex flex-col gap-2">
        <div className={`rounded-lg border px-3 py-2 ${
          deadline
            ? isExpired
              ? 'border-red-800/60 bg-red-950/30'
              : 'border-[#d4af37]/40 bg-[#d4af37]/10'
            : 'border-[#333] bg-[#252525]'
        }`}>
          <p className={`text-[10px] uppercase tracking-[0.16em] ${
            deadline
              ? isExpired
                ? 'text-red-300'
                : 'text-[#d4af37]'
              : 'text-[#888]'
          }`}>
            {deadline && isExpired ? 'Scaduto' : 'Scadenza'}
          </p>
          <p className={`text-sm font-semibold ${
            deadline
              ? isExpired
                ? 'text-red-400'
                : 'text-[#f3d67a]'
              : 'text-[#ddd]'
          }`}>
            {deadline ?? 'Non indicata'}
          </p>
        </div>
        {casting.location && (
          <p className="text-xs text-[#777] flex items-center gap-1">
            <span>📍</span> {casting.location}
          </p>
        )}
        {false && (
          <p className={`text-xs flex items-center gap-1 ${isExpired ? 'text-red-500' : 'text-[#777]'}`}>
            <span>⏰</span>
            {isExpired ? 'Scaduto: ' : 'Scadenza: '}
            <span className={isExpired ? 'text-red-400' : 'text-[#d4af37]'}>{deadline}</span>
          </p>
        )}
        {casting.description && (
          <p className="text-xs text-[#666] line-clamp-2 mt-0.5">{casting.description}</p>
        )}
      </div>

      {/* Azioni principali */}
      <div className="flex gap-2 mt-auto pt-2 border-t border-[#252525]">
        <a
          href={casting.sourceUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="flex-1 text-center text-sm py-2 px-3 rounded-lg bg-[#d4af37] text-black font-semibold hover:bg-[#b8962c] transition-colors"
        >
          Candidati →
        </a>
        <button
          onClick={handleCopyLink}
          className="text-sm py-2 px-3 rounded-lg border border-[#333] text-[#777] hover:border-blue-800/60 hover:text-blue-400 transition-colors"
          title="Copia link"
        >
          {copied ? '✓' : '🔗'}
        </button>
        {casting.isApplied ? (
          <button
            onClick={() => onUnmarkApplied(casting.id)}
            className="text-sm py-2 px-3 rounded-lg border border-[#333] text-[#777] hover:border-red-800/60 hover:text-red-400 transition-colors"
            title="Annulla candidatura"
          >
            ✕
          </button>
        ) : (
          <button
            onClick={() => onMarkApplied(casting.id)}
            className="text-sm py-2 px-3 rounded-lg border border-[#333] text-[#777] hover:border-emerald-800/60 hover:text-emerald-400 transition-colors"
            title="Conferma candidatura"
          >
            ✓
          </button>
        )}
      </div>
    </div>
  );
}
