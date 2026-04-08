import type { CastingType, SourceRegion } from '../api/types';

const TYPES: CastingType[] = ['Film', 'TV', 'Teatro', 'Pubblicità', 'Cortometraggio', 'Internazionale', 'Altro'];
const REGIONS: SourceRegion[] = ['Italy', 'Europe', 'International'];

interface Props {
  keywords: string;
  selectedTypes: CastingType[];
  selectedRegions: SourceRegion[];
  showHidden: boolean;
  onKeywordsChange: (v: string) => void;
  onToggleType: (t: CastingType) => void;
  onToggleRegion: (r: SourceRegion) => void;
  onToggleShowHidden: () => void;
  onReset: () => void;
}

export function FilterPanel({
  keywords, selectedTypes, selectedRegions, showHidden,
  onKeywordsChange, onToggleType, onToggleRegion, onToggleShowHidden, onReset
}: Props) {
  return (
    <div className="flex flex-col gap-5">
      <div>
        <label className="block text-xs font-semibold text-[#555] uppercase tracking-wider mb-2">Cerca</label>
        <input
          type="text"
          value={keywords}
          onChange={e => onKeywordsChange(e.target.value)}
          placeholder="Parole chiave..."
          className="w-full bg-[#161616] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#444] focus:outline-none focus:border-[#d4af37]/40"
        />
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#555] uppercase tracking-wider mb-2">Tipo</label>
        <div className="flex flex-col gap-1">
          {TYPES.map(type => (
            <button
              key={type}
              onClick={() => onToggleType(type)}
              className={`text-left text-xs px-2.5 py-1.5 rounded-lg transition-colors ${
                selectedTypes.includes(type)
                  ? 'bg-[#d4af37]/15 text-[#d4af37] border border-[#d4af37]/30'
                  : 'text-[#666] hover:bg-[#1e1e1e] hover:text-[#999]'
              }`}
            >
              {type}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#555] uppercase tracking-wider mb-2">Zona</label>
        <div className="flex flex-col gap-1">
          {REGIONS.map(region => (
            <button
              key={region}
              onClick={() => onToggleRegion(region)}
              className={`text-left text-xs px-2.5 py-1.5 rounded-lg transition-colors ${
                selectedRegions.includes(region)
                  ? 'bg-[#d4af37]/15 text-[#d4af37] border border-[#d4af37]/30'
                  : 'text-[#666] hover:bg-[#1e1e1e] hover:text-[#999]'
              }`}
            >
              {region === 'Italy' ? '🇮🇹 Italia' : region === 'Europe' ? '🇪🇺 Europa' : '🌍 Internazionale'}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#555] uppercase tracking-wider mb-2">Archivio</label>
        <button
          onClick={onToggleShowHidden}
          className={`w-full text-left text-xs px-2.5 py-1.5 rounded-lg transition-colors flex items-center gap-2 ${
            showHidden
              ? 'bg-[#d4af37]/15 text-[#d4af37] border border-[#d4af37]/30'
              : 'text-[#666] hover:bg-[#1e1e1e] hover:text-[#999]'
          }`}
        >
          <span>{showHidden ? '👁' : '🚫'}</span>
          {showHidden ? 'Mostra nascosti' : 'Nascosti'}
        </button>
      </div>

      {(keywords || selectedTypes.length > 0 || selectedRegions.length > 0 || showHidden) && (
        <button
          onClick={onReset}
          className="text-xs text-[#555] hover:text-[#d4af37] transition-colors underline text-left"
        >
          Reimposta filtri
        </button>
      )}
    </div>
  );
}
