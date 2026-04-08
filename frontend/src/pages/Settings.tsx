import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { SourceStatus, UserProfile } from '../api/types';

export function Settings() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [form, setForm] = useState({ gender: 'female', scenicAge: '', telegramChatId: '' });
  const [sources, setSources] = useState<SourceStatus[]>([]);
  const [togglingSource, setTogglingSource] = useState<string | null>(null);
  const [newSource, setNewSource] = useState({ name: '', url: '', region: 'Italy' });
  const [addingSource, setAddingSource] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);

  useEffect(() => {
    castingApi.getProfile().then(p => {
      setProfile(p);
      setForm({
        gender: p.gender,
        scenicAge: p.scenicAge?.toString() ?? '',
        telegramChatId: p.telegramChatId ?? '',
      });
    });
    castingApi.getSources().then(setSources);
  }, []);

  const handleToggleSource = async (name: string, enabled: boolean) => {
    setTogglingSource(name);
    try {
      await castingApi.toggleSourceEnabled(name, enabled);
      setSources(prev => prev.map(s => s.name === name ? { ...s, isEnabled: enabled } : s));
    } finally {
      setTogglingSource(null);
    }
  };

  const handleAddSource = async () => {
    setAddError(null);
    setAddingSource(true);
    try {
      const created = await castingApi.createSource(newSource.name, newSource.url, newSource.region);
      setSources(prev => [...prev, created]);
      setNewSource({ name: '', url: '', region: 'Italy' });
    } catch (e) {
      setAddError(e instanceof Error ? e.message : 'Errore');
    } finally {
      setAddingSource(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await castingApi.updateProfile({
        gender: form.gender,
        scenicAge: form.scenicAge ? parseInt(form.scenicAge) : undefined,
        telegramChatId: form.telegramChatId || undefined,
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } finally {
      setSaving(false);
    }
  };

  if (!profile) return <div className="p-8 text-[#9ca3af]">Caricamento...</div>;

  return (
    <div className="p-6 max-w-lg">
      <h2 className="text-xl font-bold text-[#f5f5f5] mb-6">⚙️ Impostazioni</h2>

      <div className="flex flex-col gap-5 bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-5">
        <div>
          <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-1.5">Genere</label>
          <select
            value={form.gender}
            onChange={e => setForm(f => ({ ...f, gender: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] focus:outline-none focus:border-[#d4af37]/50"
          >
            <option value="female">Donna</option>
            <option value="male">Uomo</option>
            <option value="any">Qualsiasi</option>
          </select>
        </div>

        <div>
          <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-1.5">Età scenica</label>
          <input
            type="number"
            value={form.scenicAge}
            onChange={e => setForm(f => ({ ...f, scenicAge: e.target.value }))}
            placeholder="Es. 30"
            min={18} max={80}
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#555] focus:outline-none focus:border-[#d4af37]/50"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-[#9ca3af] uppercase tracking-wide mb-1.5">
            Telegram Chat ID
          </label>
          <input
            type="text"
            value={form.telegramChatId}
            onChange={e => setForm(f => ({ ...f, telegramChatId: e.target.value }))}
            placeholder="Es. 123456789"
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#555] focus:outline-none focus:border-[#d4af37]/50"
          />
          <p className="text-xs text-[#555] mt-1">Ottieni il tuo Chat ID con @userinfobot su Telegram</p>
        </div>

        <button
          onClick={handleSave}
          disabled={saving}
          className="py-2 px-4 rounded-lg bg-[#d4af37] text-black font-medium hover:bg-[#b8962c] transition-colors disabled:opacity-50"
        >
          {saved ? '✓ Salvato' : saving ? 'Salvataggio...' : 'Salva impostazioni'}
        </button>
      </div>

      <div className="mt-6 flex flex-col gap-4 bg-[#1e1e1e] border border-[#2a2a2a] rounded-xl p-5">
        <h3 className="text-sm font-semibold text-[#9ca3af] uppercase tracking-wide">Siti da cercare</h3>

        {sources.map(s => (
          <div key={s.name} className="flex items-center justify-between gap-3">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="text-sm text-[#f5f5f5]">{s.name}</span>
                <span className="text-xs text-[#555]">{s.region}</span>
              </div>
              {s.url && (
                <a
                  href={s.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-[#555] hover:text-[#d4af37] truncate block"
                >
                  {s.url}
                </a>
              )}
            </div>
            <button
              onClick={() => handleToggleSource(s.name, !s.isEnabled)}
              disabled={togglingSource === s.name}
              className={`flex-shrink-0 relative w-10 h-5 rounded-full transition-colors disabled:opacity-50 ${
                s.isEnabled ? 'bg-[#d4af37]' : 'bg-[#2a2a2a]'
              }`}
            >
              <span className={`absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white transition-transform ${
                s.isEnabled ? 'translate-x-5' : 'translate-x-0'
              }`} />
            </button>
          </div>
        ))}

        <div className="border-t border-[#2a2a2a] pt-4 flex flex-col gap-2">
          <p className="text-xs font-semibold text-[#9ca3af] uppercase tracking-wide">Aggiungi sito</p>
          <input
            type="text"
            placeholder="Nome (es. MyCasting)"
            value={newSource.name}
            onChange={e => setNewSource(s => ({ ...s, name: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#555] focus:outline-none focus:border-[#d4af37]/50"
          />
          <input
            type="url"
            placeholder="URL (es. https://www.mycasting.it/casting)"
            value={newSource.url}
            onChange={e => setNewSource(s => ({ ...s, url: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#555] focus:outline-none focus:border-[#d4af37]/50"
          />
          <select
            value={newSource.region}
            onChange={e => setNewSource(s => ({ ...s, region: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] focus:outline-none focus:border-[#d4af37]/50"
          >
            <option value="Italy">Italy</option>
            <option value="Europe">Europe</option>
            <option value="International">International</option>
          </select>
          {addError && <p className="text-xs text-red-400">{addError}</p>}
          <button
            onClick={handleAddSource}
            disabled={addingSource || !newSource.name || !newSource.url}
            className="py-2 px-4 rounded-lg border border-[#d4af37]/40 text-[#d4af37] text-sm font-medium hover:bg-[#d4af37]/10 transition-colors disabled:opacity-50"
          >
            {addingSource ? 'Aggiunta...' : '+ Aggiungi sito'}
          </button>
        </div>
      </div>
    </div>
  );
}
