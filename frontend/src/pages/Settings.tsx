import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { SourceStatus, UserProfile } from '../api/types';
import { getStoredInterval, setStoredInterval, DEFAULT_INTERVAL } from '../hooks/useAutoRefresh';

export function Settings() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [form, setForm] = useState({ gender: 'female', scenicAge: '', telegramChatId: '' });

  const [refreshInterval, setRefreshIntervalState] = useState<number>(getStoredInterval);
  const [intervalSaved, setIntervalSaved] = useState(false);

  const [sources, setSources] = useState<SourceStatus[]>([]);
  const [togglingSource, setTogglingSource] = useState<string | null>(null);
  const [deletingSource, setDeletingSource] = useState<string | null>(null);
  const [newSource, setNewSource] = useState({ name: '', url: '', region: 'Italy' });
  const [addingSource, setAddingSource] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);

  useEffect(() => {
    castingApi.getProfile().then(p => {
      setProfile(p);
      setForm({ gender: p.gender, scenicAge: p.scenicAge?.toString() ?? '', telegramChatId: p.telegramChatId ?? '' });
    });
    castingApi.getSources().then(setSources);
  }, []);

  const handleSaveInterval = () => {
    const clamped = Math.max(10, Math.min(3600, refreshInterval));
    setStoredInterval(clamped);
    setRefreshIntervalState(clamped);
    setIntervalSaved(true);
    setTimeout(() => setIntervalSaved(false), 2000);
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

  const handleToggleSource = async (name: string, enabled: boolean) => {
    setTogglingSource(name);
    try {
      await castingApi.toggleSourceEnabled(name, enabled);
      setSources(prev => prev.map(s => s.name === name ? { ...s, isEnabled: enabled } : s));
    } finally {
      setTogglingSource(null);
    }
  };

  const handleDeleteSource = async (name: string) => {
    if (!confirm(`Eliminare il sito "${name}"?`)) return;
    setDeletingSource(name);
    try {
      await castingApi.deleteSource(name);
      setSources(prev => prev.filter(s => s.name !== name));
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Errore durante l\'eliminazione');
    } finally {
      setDeletingSource(null);
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

  if (!profile) return <div className="p-8 text-[#555]">Caricamento...</div>;

  const builtInSources = sources.filter(s => s.hasCustomScraper);
  const customSources = sources.filter(s => !s.hasCustomScraper);

  return (
    <div className="p-6 max-w-2xl overflow-y-auto h-full">
      <h2 className="text-lg font-bold text-[#f5f5f5] mb-5">Impostazioni</h2>

      {/* Profilo */}
      <section className="flex flex-col gap-4 bg-[#141414] border border-[#1e1e1e] rounded-xl p-5 mb-5">
        <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider">Il tuo profilo</h3>

        <div>
          <label className="block text-xs text-[#666] mb-1">Genere</label>
          <select
            value={form.gender}
            onChange={e => setForm(f => ({ ...f, gender: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] focus:outline-none focus:border-[#d4af37]/40"
          >
            <option value="female">Donna</option>
            <option value="male">Uomo</option>
            <option value="any">Qualsiasi</option>
          </select>
        </div>

        <div>
          <label className="block text-xs text-[#666] mb-1">Età scenica</label>
          <input
            type="number"
            value={form.scenicAge}
            onChange={e => setForm(f => ({ ...f, scenicAge: e.target.value }))}
            placeholder="Es. 30"
            min={18} max={80}
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#444] focus:outline-none focus:border-[#d4af37]/40"
          />
        </div>

        <div>
          <label className="block text-xs text-[#666] mb-1">Telegram Chat ID</label>
          <input
            type="text"
            value={form.telegramChatId}
            onChange={e => setForm(f => ({ ...f, telegramChatId: e.target.value }))}
            placeholder="Es. 123456789"
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#444] focus:outline-none focus:border-[#d4af37]/40"
          />
          <p className="text-xs text-[#444] mt-1">Ricevi notifiche su Telegram. Ottieni l'ID con @userinfobot</p>
        </div>

        <button
          onClick={handleSave}
          disabled={saving}
          className="py-2 px-4 rounded-lg bg-[#d4af37] text-black text-sm font-semibold hover:bg-[#b8962c] transition-colors disabled:opacity-50"
        >
          {saved ? '✓ Salvato' : saving ? 'Salvataggio...' : 'Salva'}
        </button>
      </section>

      {/* Aggiornamento automatico */}
      <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5 mb-5">
        <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-1">Aggiornamento automatico</h3>
        <p className="text-xs text-[#444] mb-4">
          Ogni quanti secondi cercare automaticamente nuovi casting in background.
        </p>
        <div className="flex items-center gap-3">
          <input
            type="number"
            min={10}
            max={3600}
            value={refreshInterval}
            onChange={e => setRefreshIntervalState(Number(e.target.value))}
            className="w-28 bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] focus:outline-none focus:border-[#d4af37]/40"
          />
          <span className="text-xs text-[#555]">secondi (min 10)</span>
          <button
            onClick={handleSaveInterval}
            className="py-2 px-3 rounded-lg bg-[#d4af37] text-black text-xs font-semibold hover:bg-[#b8962c] transition-colors"
          >
            {intervalSaved ? '✓ Salvato' : 'Applica'}
          </button>
        </div>
        <p className="text-xs text-[#3a3a3a] mt-2">
          Suggerimento: 60s per uso normale, 300s per risparmio batteria.
          Il timer è visibile nell'icona in alto a destra — clicca per aggiornare subito.
        </p>
      </section>

      {/* Siti integrati */}
      <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5 mb-5">
        <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-1">Siti integrati</h3>
        <p className="text-xs text-[#444] mb-4">Questi siti hanno uno scraper dedicato e funzionano al meglio.</p>

        <div className="flex flex-col gap-3">
          {builtInSources.map(s => (
            <div key={s.name} className="flex items-center justify-between gap-3">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="text-sm text-[#d5d5d5]">{s.name}</span>
                  <span className="text-xs text-[#3a7b47] bg-[#1a3a22] px-1.5 py-0.5 rounded">✓ Integrato</span>
                </div>
                {s.lastScrapedAt && (
                  <p className="text-xs text-[#444] mt-0.5">
                    Ultimo aggiornamento: {new Date(s.lastScrapedAt).toLocaleDateString('it-IT')}
                  </p>
                )}
              </div>
              <Toggle
                enabled={s.isEnabled}
                loading={togglingSource === s.name}
                onChange={v => handleToggleSource(s.name, v)}
              />
            </div>
          ))}
          {builtInSources.length === 0 && (
            <p className="text-xs text-[#444]">Nessun sito integrato disponibile.</p>
          )}
        </div>
      </section>

      {/* Siti personalizzati */}
      <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
        <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-1">Siti personalizzati</h3>
        <p className="text-xs text-[#444] mb-4">
          Siti aggiunti manualmente. I risultati potrebbero essere incompleti poiché non hanno uno scraper dedicato.
        </p>

        <div className="flex flex-col gap-3 mb-4">
          {customSources.map(s => (
            <div key={s.name} className="flex items-start gap-3 p-3 bg-[#0f0f0f] rounded-lg border border-[#1e1e1e]">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm text-[#d5d5d5]">{s.name}</span>
                  <span className="text-xs text-[#7a6020] bg-[#2a2010] px-1.5 py-0.5 rounded">⚠ Generico</span>
                </div>
                {s.url && (
                  <a
                    href={s.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-[#444] hover:text-[#d4af37] truncate block mt-0.5"
                  >
                    {s.url}
                  </a>
                )}
              </div>
              <div className="flex items-center gap-2 flex-shrink-0">
                <Toggle
                  enabled={s.isEnabled}
                  loading={togglingSource === s.name}
                  onChange={v => handleToggleSource(s.name, v)}
                />
                <button
                  onClick={() => handleDeleteSource(s.name)}
                  disabled={deletingSource === s.name}
                  className="text-xs text-[#444] hover:text-red-400 transition-colors p-1 disabled:opacity-50"
                  title="Elimina sito"
                >
                  🗑
                </button>
              </div>
            </div>
          ))}
          {customSources.length === 0 && (
            <p className="text-xs text-[#444]">Nessun sito personalizzato aggiunto.</p>
          )}
        </div>

        {/* Form aggiunta */}
        <div className="border-t border-[#1e1e1e] pt-4 flex flex-col gap-2">
          <p className="text-xs font-semibold text-[#555] uppercase tracking-wider">Aggiungi un sito</p>
          <input
            type="text"
            placeholder="Nome del sito (es. MyCasting)"
            value={newSource.name}
            onChange={e => setNewSource(s => ({ ...s, name: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#444] focus:outline-none focus:border-[#d4af37]/40"
          />
          <input
            type="url"
            placeholder="URL pagina casting (es. https://sito.it/casting)"
            value={newSource.url}
            onChange={e => setNewSource(s => ({ ...s, url: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] placeholder-[#444] focus:outline-none focus:border-[#d4af37]/40"
          />
          <select
            value={newSource.region}
            onChange={e => setNewSource(s => ({ ...s, region: e.target.value }))}
            className="w-full bg-[#0f0f0f] border border-[#252525] rounded-lg px-3 py-2 text-sm text-[#f5f5f5] focus:outline-none focus:border-[#d4af37]/40"
          >
            <option value="Italy">🇮🇹 Italia</option>
            <option value="Europe">🇪🇺 Europa</option>
            <option value="International">🌍 Internazionale</option>
          </select>
          {addError && <p className="text-xs text-red-400">{addError}</p>}
          <button
            onClick={handleAddSource}
            disabled={addingSource || !newSource.name.trim() || !newSource.url.trim()}
            className="py-2 px-4 rounded-lg border border-[#d4af37]/30 text-[#d4af37] text-sm font-medium hover:bg-[#d4af37]/10 transition-colors disabled:opacity-50"
          >
            {addingSource ? 'Aggiunta...' : '+ Aggiungi sito'}
          </button>
          <p className="text-xs text-[#3a3a3a]">
            ⚠ I siti aggiunti manualmente usano uno scraper generico. Per risultati migliori, richiedi uno scraper dedicato allo sviluppatore.
          </p>
        </div>
      </section>
    </div>
  );
}

function Toggle({ enabled, loading, onChange }: { enabled: boolean; loading: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      onClick={() => onChange(!enabled)}
      disabled={loading}
      className={`relative w-9 h-5 rounded-full transition-colors flex-shrink-0 disabled:opacity-50 ${
        enabled ? 'bg-[#d4af37]' : 'bg-[#252525]'
      }`}
    >
      <span className={`absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white transition-transform ${
        enabled ? 'translate-x-4' : 'translate-x-0'
      }`} />
    </button>
  );
}
