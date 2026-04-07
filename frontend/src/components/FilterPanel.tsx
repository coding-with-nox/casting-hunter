import type { CastingType, SourceRegion } from '../api/types';

const TYPES: CastingType[] = ['Film', 'TV', 'Teatro', 'Pubblicità', 'Cortometraggio', 'Internazionale', 'Altro'];
const REGIONS: SourceRegion[] = ['Italy', 'Europe', 'International'];

interface Props {
  keywords: string;
  selectedTypes: CastingType[];
  selectedRegions: SourceRegion[];
  onlyPaid: boolean;
  onKeywordsChange: (v: string) => void;
  onToggleType: (t: CastingType) => void;
  onToggleRegion: (r: SourceRegion) => void;
  onTogglePaid: (v: boolean) => void;
  onReset: () => void;
}

export function FilterPanel({
  keywords, selectedTypes, selectedRegions, onlyPaid,
  onKeywordsChange, onToggleType, onToggleRegion, onTogglePaid, onReset
}: Props) {
  return (
    <aside className="w-64 flex-shrink-0 flex flex-col gap-5">
      <div>
        <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-2">Ricerca</label>
        <input
          type="text"
          value={keywords}
          onChange={e => onKeywordsChange(e.target.value)}
          placeholder="Parole chiave..."
          className="w-full bg-[#1e1e1e] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#555] focus:outline-none focus:border-[#d4af37]/50"
        />
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-2">Tipo</label>
        <div className="flex flex-col gap-1.5">
          {TYPES.map(type => (
            <button
              key={type}
              onClick={() => onToggleType(type)}
              className={`text-left text-sm px-3 py-1.5 rounded-lg transition-colors ${
                selectedTypes.includes(type)
                  ? 'bg-[#d4af37]/20 text-[#d4af37] border border-[#d4af37]/40'
                  : 'text-[#9ca3af] hover:bg-[#2a2a2a]'
              }`}
            >
              {type}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-2">Regione</label>
        <div className="flex flex-col gap-1.5">
          {REGIONS.map(region => (
            <button
              key={region}
              onClick={() => onToggleRegion(region)}
              className={`text-left text-sm px-3 py-1.5 rounded-lg transition-colors ${
                selectedRegions.includes(region)
                  ? 'bg-[#d4af37]/20 text-[#d4af37] border border-[#d4af37]/40'
                  : 'text-[#9ca3af] hover:bg-[#2a2a2a]'
              }`}
            >
              {region}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={onlyPaid}
            onChange={e => onTogglePaid(e.target.checked)}
            className="accent-[#d4af37]"
          />
          <span className="text-sm text-[#9ca3af]">Solo retribuiti</span>
        </label>
      </div>

      <button
        onClick={onReset}
        className="text-sm text-[#9ca3af] hover:text-[#d4af37] transition-colors underline text-left"
      >
        Reimposta filtri
      </button>
    </aside>
  );
}
