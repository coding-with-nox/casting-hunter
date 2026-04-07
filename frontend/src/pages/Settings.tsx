import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { UserProfile } from '../api/types';

export function Settings() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [form, setForm] = useState({ gender: 'female', scenicAge: '', telegramChatId: '' });

  useEffect(() => {
    castingApi.getProfile().then(p => {
      setProfile(p);
      setForm({
        gender: p.gender,
        scenicAge: p.scenicAge?.toString() ?? '',
        telegramChatId: p.telegramChatId ?? '',
      });
    });
  }, []);

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
    </div>
  );
}
