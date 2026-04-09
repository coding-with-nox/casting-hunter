import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { BandiPlan, Bando, BandoScrapeResult, BandoSource } from '../api/types';

const FALLBACK_PLAN: BandiPlan = {
  status: 'Planning locale',
  summary: 'Roadmap iniziale per integrare i bandi di concorso artistici con fonti ufficiali, parsing PDF e classificazione per ente.',
  priorityPlan: [],
  extractionFlow: [],
  issuerTypes: [],
  sourceGroups: [],
};

type SortMode = 'recent' | 'deadline' | 'confidence';
type ConfidenceFilter = 'all' | 'high' | 'medium' | 'low';
type UserStatusFilter = 'all' | 'Considerato' | 'Escluso';
type BandiTab = 'dashboard' | 'impostazioni';

function formatDate(value: string | null) {
  if (!value) return 'Non indicata';
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return 'Non indicata';
  return parsed.toLocaleDateString('it-IT');
}

function getStatusTone(status: string) {
  if (status === 'Scaduto') return 'border-red-800/40 bg-red-900/25 text-red-300';
  if (status === 'Da rivedere') return 'border-amber-800/40 bg-amber-900/25 text-amber-200';
  return 'border-emerald-800/40 bg-emerald-900/25 text-emerald-300';
}

function getDeadlineTone(deadline: string | null) {
  if (!deadline) return 'border-[#2a2a2a] bg-[#161616] text-[#999]';
  const parsed = new Date(deadline);
  if (Number.isNaN(parsed.getTime())) return 'border-[#2a2a2a] bg-[#161616] text-[#999]';
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const diffDays = Math.ceil((parsed.getTime() - today.getTime()) / 86400000);
  if (diffDays < 0) return 'border-red-800/40 bg-red-900/25 text-red-300';
  if (diffDays <= 10) return 'border-amber-800/40 bg-amber-900/25 text-amber-200';
  return 'border-[#d4af37]/30 bg-[#d4af37]/12 text-[#f3d67a]';
}

function getPreview(bodyText: string) {
  const normalized = bodyText.replace(/\s+/g, ' ').trim();
  if (normalized.length <= 170) return normalized;
  return `${normalized.slice(0, 167)}...`;
}

function getUserStatusTone(userStatus: Bando['userStatus']) {
  if (userStatus === 'Considerato') return 'border-emerald-700/40 bg-emerald-900/20 text-emerald-300';
  if (userStatus === 'Escluso') return 'border-red-800/40 bg-red-900/20 text-red-300';
  return 'border-[#2a2a2a] bg-[#171717] text-[#bdbdbd]';
}

function getUserStatusLabel(userStatus: Bando['userStatus']) {
  if (userStatus === 'Considerato') return 'Considerato';
  if (userStatus === 'Escluso') return 'Escluso';
  return 'Non valutato';
}

export function BandiPhase3() {
  const [bandiTab, setBandiTab] = useState<BandiTab>('dashboard');

  // Data
  const [plan, setPlan] = useState<BandiPlan | null>(null);
  const [bandi, setBandi] = useState<Bando[]>([]);
  const [sources, setSources] = useState<BandoSource[]>([]);
  const [loading, setLoading] = useState(true);
  const [warnings, setWarnings] = useState<string[]>([]);

  // Bando detail
  const [selectedBandoId, setSelectedBandoId] = useState<string | null>(null);
  const [selectedBando, setSelectedBando] = useState<Bando | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  // Scraping
  const [scraping, setScraping] = useState(false);
  const [scrapeResult, setScrapeResult] = useState<BandoScrapeResult | null>(null);
  const [scrapeScope, setScrapeScope] = useState<'P1' | 'P2' | 'P3' | null>(null);

  // Filters (dashboard)
  const [search, setSearch] = useState('');
  const [sourceFilter, setSourceFilter] = useState('all');
  const [issuerTypeFilter, setIssuerTypeFilter] = useState('all');
  const [disciplineFilter, setDisciplineFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortMode, setSortMode] = useState<SortMode>('recent');
  const [confidenceFilter, setConfidenceFilter] = useState<ConfidenceFilter>('all');
  const [onlyPublic, setOnlyPublic] = useState(false);
  const [userStatusFilter, setUserStatusFilter] = useState<UserStatusFilter>('all');

  // Sources (impostazioni)
  const [regioneFilter, setRegioneFilter] = useState('all');
  const [editingSource, setEditingSource] = useState<string | null>(null);
  const [editBaseUrl, setEditBaseUrl] = useState('');
  const [editRegione, setEditRegione] = useState('');
  const [savingSource, setSavingSource] = useState(false);
  const [curatedName, setCuratedName] = useState('');
  const [curatedBaseUrl, setCuratedBaseUrl] = useState('');
  const [savingCurated, setSavingCurated] = useState(false);

  const load = async () => {
    setLoading(true);
    const nextWarnings: string[] = [];
    try {
      const [planResult, bandiResult, sourcesResult] = await Promise.allSettled([
        castingApi.getBandiPlan(),
        castingApi.getBandi(),
        castingApi.getBandiSources(),
      ]);
      setPlan(planResult.status === 'fulfilled' ? planResult.value : FALLBACK_PLAN);
      if (planResult.status === 'rejected')
        nextWarnings.push(planResult.reason instanceof Error ? planResult.reason.message : 'Piano bandi non disponibile');
      setBandi(bandiResult.status === 'fulfilled' ? bandiResult.value : []);
      if (bandiResult.status === 'rejected')
        nextWarnings.push(bandiResult.reason instanceof Error ? bandiResult.reason.message : 'Archivio bandi non disponibile');
      setSources(sourcesResult.status === 'fulfilled' ? sourcesResult.value : []);
      if (sourcesResult.status === 'rejected')
        nextWarnings.push(sourcesResult.reason instanceof Error ? sourcesResult.reason.message : 'Registry fonti non disponibile');
      setWarnings(nextWarnings);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  // Derived lists
  const sourceOptions = Array.from(new Set(bandi.map(b => b.sourceName))).sort((a, b) => a.localeCompare(b));
  const issuerTypeOptions = Array.from(new Set(bandi.map(b => b.issuerType))).sort((a, b) => a.localeCompare(b));
  const disciplineOptions = Array.from(new Set(bandi.flatMap(b => b.discipline ? [b.discipline] : []))).sort((a, b) => a.localeCompare(b));
  const statusOptions = Array.from(new Set(bandi.map(b => b.status))).sort((a, b) => a.localeCompare(b));
  const curatedSources = sources.filter(s => s.category.includes('P3'));
  const regioneOptions = Array.from(new Set(sources.map(s => s.regione).filter(Boolean) as string[])).sort((a, b) => a.localeCompare(b));
  const filteredSources = regioneFilter === 'all' ? sources : sources.filter(s => s.regione === regioneFilter);

  const filteredBandi = [...bandi]
    .filter(b => {
      const hay = `${b.title} ${b.issuerName} ${b.role ?? ''} ${b.bodyText}`.toLowerCase();
      const needle = search.trim().toLowerCase();
      if (needle && !hay.includes(needle)) return false;
      if (sourceFilter !== 'all' && b.sourceName !== sourceFilter) return false;
      if (issuerTypeFilter !== 'all' && b.issuerType !== issuerTypeFilter) return false;
      if (disciplineFilter !== 'all' && b.discipline !== disciplineFilter) return false;
      if (statusFilter !== 'all' && b.status !== statusFilter) return false;
      if (confidenceFilter === 'high' && b.confidenceScore < 0.85) return false;
      if (confidenceFilter === 'medium' && (b.confidenceScore < 0.70 || b.confidenceScore >= 0.85)) return false;
      if (confidenceFilter === 'low' && b.confidenceScore >= 0.70) return false;
      if (onlyPublic && !b.isPublic) return false;
       if (userStatusFilter === 'Considerato' && b.userStatus !== 'Considerato') return false;
       if (userStatusFilter === 'Escluso' && b.userStatus !== 'Escluso') return false;
       return true;
    })
    .sort((l, r) => {
      if (sortMode === 'confidence') return l.confidenceScore - r.confidenceScore;
      if (sortMode === 'deadline') {
        const ld = l.deadline ? new Date(l.deadline).getTime() : Number.MAX_SAFE_INTEGER;
        const rd = r.deadline ? new Date(r.deadline).getTime() : Number.MAX_SAFE_INTEGER;
        if (ld !== rd) return ld - rd;
      }
      return new Date(r.publishedAt ?? r.createdAt).getTime() - new Date(l.publishedAt ?? l.createdAt).getTime();
    });

  useEffect(() => {
    if (filteredBandi.length === 0) {
      setSelectedBandoId(null);
      setSelectedBando(null);
      return;
    }
    if (selectedBandoId && !filteredBandi.some(b => b.id === selectedBandoId)) {
      setSelectedBandoId(null);
      setSelectedBando(null);
    }
  }, [filteredBandi, selectedBandoId]);

  useEffect(() => {
    if (!selectedBandoId) return;
    const fallback = bandi.find(b => b.id === selectedBandoId) ?? null;
    if (fallback) setSelectedBando(fallback);
    let active = true;
    const go = async () => {
      setDetailLoading(true);
      try {
        const detail = await castingApi.getBando(selectedBandoId);
        if (active) setSelectedBando(detail);
      } catch {
        if (active && fallback) setSelectedBando(fallback);
      } finally {
        if (active) setDetailLoading(false);
      }
    };
    void go();
    return () => { active = false; };
  }, [selectedBandoId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Scrape handlers
  const handleScrape = async (scope: 'P1' | 'P2' | 'P3') => {
    setScraping(true); setWarnings([]); setScrapeScope(scope);
    try {
      const result = scope === 'P1' ? await castingApi.scrapeBandiP1()
        : scope === 'P2' ? await castingApi.scrapeBandiP2()
        : await castingApi.scrapeBandiP3();
      setScrapeResult(result);
      await load();
    } catch (error) {
      setScrapeResult(null);
      setWarnings([error instanceof Error ? error.message : `Scrape ${scope} non disponibile`]);
    } finally {
      setScraping(false);
    }
  };

  // UserStatus handler
  const handleSetUserStatus = async (id: string, status: 'Considerato' | 'Escluso' | null) => {
    try {
      const updated = await castingApi.setBandoUserStatus(id, status);
      setBandi(prev => prev.map(b => b.id === id ? { ...b, userStatus: updated.userStatus } : b));
      if (selectedBando?.id === id) setSelectedBando(prev => prev ? { ...prev, userStatus: updated.userStatus } : prev);
    } catch (error) {
      setWarnings([error instanceof Error ? error.message : 'Errore aggiornamento stato']);
    }
  };

  const closeDetail = () => {
    setSelectedBandoId(null);
    setSelectedBando(null);
  };

  // Source handlers
  const handleEditSource = (source: BandoSource) => {
    setEditingSource(source.name); setEditBaseUrl(source.baseUrl); setEditRegione(source.regione ?? '');
  };
  const handleSaveSource = async (name: string) => {
    setSavingSource(true);
    try {
      await castingApi.updateBandoSource(name, { baseUrl: editBaseUrl, regione: editRegione });
      setEditingSource(null); await load();
    } catch (error) {
      setWarnings([error instanceof Error ? error.message : 'Errore salvataggio fonte']);
    } finally { setSavingSource(false); }
  };
  const handleDeleteSource = async (name: string) => {
    if (!confirm(`Eliminare la fonte "${name}"?`)) return;
    try { await castingApi.deleteBandoSource(name); await load(); }
    catch (error) { setWarnings([error instanceof Error ? error.message : 'Errore eliminazione fonte']); }
  };
  const handleToggleBandoSource = async (name: string, enabled: boolean) => {
    try { await castingApi.toggleBandoSourceEnabled(name, enabled); await load(); }
    catch (error) { setWarnings([error instanceof Error ? error.message : 'Errore toggle fonte']); }
  };
  const handleCreateCuratedSource = async () => {
    if (!curatedName.trim() || !curatedBaseUrl.trim()) {
      setWarnings(['Nome e URL obbligatori']); return;
    }
    setSavingCurated(true); setWarnings([]);
    try {
      await castingApi.createCuratedBandoSource({ name: curatedName.trim(), baseUrl: curatedBaseUrl.trim(), priority: 20, isOfficial: false });
      setCuratedName(''); setCuratedBaseUrl(''); await load();
    } catch (error) {
      setWarnings([error instanceof Error ? error.message : 'Registrazione fonte non disponibile']);
    } finally { setSavingCurated(false); }
  };

  if (loading) return <div className="p-8 text-[#666]">Caricamento bandi...</div>;

  const inputCls = "rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40";

  return (
    <div className="h-full flex flex-col overflow-hidden">
      {/* Sub-tab bar */}
      <div className="flex-shrink-0 border-b border-[#1a1a1a] bg-[#0d0d0d] px-5">
        <div className="flex gap-0.5">
          {(['dashboard', 'impostazioni'] as BandiTab[]).map(t => (
            <button
              key={t}
              type="button"
              onClick={() => setBandiTab(t)}
              className={`px-4 py-2.5 text-sm font-medium capitalize transition-colors border-b-2 -mb-px ${
                bandiTab === t
                  ? 'border-[#d4af37] text-[#f3d67a]'
                  : 'border-transparent text-[#555] hover:text-[#999]'
              }`}
            >
              {t === 'dashboard' ? 'Dashboard' : 'Impostazioni'}
            </button>
          ))}
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 min-h-0 overflow-y-auto">
        {warnings.length > 0 && (
          <div className="mx-6 mt-4 rounded-lg border border-yellow-800/50 bg-yellow-900/20 p-3 text-sm text-yellow-200 flex flex-col gap-1">
            {warnings.map(w => <div key={w}>{w}</div>)}
          </div>
        )}

        {/* ── DASHBOARD ── */}
        {bandiTab === 'dashboard' && (
          <div className="p-6 flex flex-col gap-5">
            {/* Header stats */}
            <section className="grid grid-cols-2 gap-3 md:grid-cols-4">
              <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-4">
                <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Archivio</p>
                <p className="mt-1 text-2xl font-semibold text-[#f5f5f5]">{bandi.length}</p>
              </div>
              <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-4">
                <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Visibili</p>
                <p className="mt-1 text-2xl font-semibold text-[#f5f5f5]">{filteredBandi.length}</p>
              </div>
              <div className="rounded-xl border border-emerald-800/30 bg-[#0d1a12] p-4">
                <p className="text-[10px] uppercase tracking-[0.16em] text-emerald-600">Considerati</p>
                <p className="mt-1 text-2xl font-semibold text-emerald-300">{bandi.filter(b => b.userStatus === 'Considerato').length}</p>
              </div>
              <div className="rounded-xl border border-red-900/30 bg-[#160d0d] p-4">
                <p className="text-[10px] uppercase tracking-[0.16em] text-red-700">Esclusi</p>
                <p className="mt-1 text-2xl font-semibold text-red-400">{bandi.filter(b => b.userStatus === 'Escluso').length}</p>
              </div>
            </section>

            {/* Scrape result */}
            {scrapeResult && (
              <section className="grid grid-cols-4 gap-3">
                {[
                  { label: 'Ambito', value: scrapeScope ?? '-', color: 'text-[#f5f5f5]' },
                  { label: 'Trovati', value: scrapeResult.totalFound, color: 'text-[#f5f5f5]' },
                  { label: 'Artistici', value: scrapeResult.totalEligible, color: 'text-[#f5f5f5]' },
                  { label: 'Nuovi', value: scrapeResult.totalNew, color: 'text-[#f3d67a]' },
                ].map(({ label, value, color }) => (
                  <div key={label} className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-4">
                    <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">{label}</p>
                    <p className={`mt-1 text-2xl font-semibold ${color}`}>{value}</p>
                  </div>
                ))}
              </section>
            )}

            {/* Filters */}
            <section className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-4">
              <div className="grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-7">
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Ricerca</span>
                  <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Titolo, ente, ruolo" className={inputCls} />
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Fonte</span>
                  <select value={sourceFilter} onChange={e => setSourceFilter(e.target.value)} className={inputCls}>
                    <option value="all">Tutte</option>
                    {sourceOptions.map(o => <option key={o} value={o}>{o}</option>)}
                  </select>
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Tipo ente</span>
                  <select value={issuerTypeFilter} onChange={e => setIssuerTypeFilter(e.target.value)} className={inputCls}>
                    <option value="all">Tutti</option>
                    {issuerTypeOptions.map(o => <option key={o} value={o}>{o}</option>)}
                  </select>
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Disciplina</span>
                  <select value={disciplineFilter} onChange={e => setDisciplineFilter(e.target.value)} className={inputCls}>
                    <option value="all">Tutte</option>
                    {disciplineOptions.map(o => <option key={o} value={o}>{o}</option>)}
                  </select>
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Stato</span>
                  <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className={inputCls}>
                    <option value="all">Tutti</option>
                    {statusOptions.map(o => <option key={o} value={o}>{o}</option>)}
                  </select>
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Ordina</span>
                  <select value={sortMode} onChange={e => setSortMode(e.target.value as SortMode)} className={inputCls}>
                    <option value="recent">Più recenti</option>
                    <option value="deadline">Scadenza</option>
                    <option value="confidence">Confidenza</option>
                  </select>
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Considerazione</span>
                  <select value={userStatusFilter} onChange={e => setUserStatusFilter(e.target.value as UserStatusFilter)} className={inputCls}>
                    <option value="all">Tutti</option>
                    <option value="Considerato">Considerato</option>
                    <option value="Escluso">Escluso</option>
                  </select>
                </label>
              </div>
              <div className="mt-3 flex flex-wrap items-center gap-3">
                <label className="inline-flex items-center gap-2 rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#d0d0d0]">
                  <input type="checkbox" checked={onlyPublic} onChange={e => setOnlyPublic(e.target.checked)} className="accent-[#d4af37]" />
                  Solo pubblici
                </label>
                <button
                  type="button"
                  onClick={() => { setSearch(''); setSourceFilter('all'); setIssuerTypeFilter('all'); setDisciplineFilter('all'); setStatusFilter('all'); setSortMode('recent'); setConfidenceFilter('all'); setOnlyPublic(false); setUserStatusFilter('all'); }}
                  className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#d0d0d0] transition hover:border-[#d4af37]/40"
                >
                  Reset filtri
                </button>
              </div>
            </section>

            {/* List + Detail */}
            <section className="grid grid-cols-1 gap-5 xl:grid-cols-[1.1fr_0.9fr] items-start">
              {/* List */}
              <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Archivio bandi</h3>
                  <p className="text-xs text-[#777]">{filteredBandi.length} risultati</p>
                </div>
                {filteredBandi.length === 0 ? (
                  <div className="rounded-xl border border-dashed border-[#2a2a2a] bg-[#101010] px-4 py-6">
                    <p className="text-sm font-semibold text-[#f5f5f5]">Nessun risultato</p>
                    <p className="mt-1 text-sm text-[#7a7a7a]">Cambia filtri o esegui uno scrape.</p>
                  </div>
                ) : (
                  <div className="flex flex-col gap-3">
                    {filteredBandi.map(bando => (
                      <button
                        key={bando.id}
                        type="button"
                        onClick={() => setSelectedBandoId(bando.id)}
                        className={`w-full rounded-xl border px-4 py-4 text-left transition ${
                          selectedBandoId === bando.id
                            ? 'border-[#d4af37]/40 bg-[#15120a]'
                            : bando.userStatus === 'Escluso'
                              ? 'border-[#1e1010] bg-[#0d0808] opacity-50 hover:opacity-70'
                              : bando.userStatus === 'Considerato'
                                ? 'border-emerald-800/40 bg-[#0a110e] hover:border-emerald-700/50'
                                : 'border-[#222] bg-[#101010] hover:border-[#343434]'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="min-w-0">
                            <p className="text-sm font-semibold text-[#f5f5f5]">{bando.title}</p>
                            <p className="mt-0.5 text-xs text-[#8a8a8a]">{bando.issuerName} — {bando.sourceName}</p>
                            <p className="mt-2 text-xs text-[#9a9a9a]">{getPreview(bando.bodyText)}</p>
                          </div>
                          <div className={`min-w-[100px] rounded-xl border px-3 py-2 text-right flex-shrink-0 ${getDeadlineTone(bando.deadline)}`}>
                            <p className="text-[10px] uppercase tracking-[0.14em]">Scadenza</p>
                            <p className="mt-1 text-xs font-semibold">{formatDate(bando.deadline)}</p>
                          </div>
                        </div>
                        <div className="mt-3 flex flex-wrap gap-1.5 text-[11px]">
                          <span className={`rounded-md border px-2 py-0.5 ${getStatusTone(bando.status)}`}>{bando.status}</span>
                          {bando.discipline && <span className="rounded-md border border-sky-900/40 bg-sky-900/20 px-2 py-0.5 text-sky-200">{bando.discipline}</span>}
                          <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-0.5 text-[#bdbdbd]">Conf. {bando.confidenceScore.toFixed(2)}</span>
                          {bando.userStatus === 'Considerato' && <span className="rounded-md border border-emerald-700/40 bg-emerald-900/20 px-2 py-0.5 text-emerald-300">✓ Considerato</span>}
                          {bando.userStatus === 'Escluso' && <span className="rounded-md border border-red-800/40 bg-red-900/20 px-2 py-0.5 text-red-300">✕ Escluso</span>}
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>

              {/* Detail */}
              <div className="sticky top-0 rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Dettaglio</h3>
                  {detailLoading && <span className="text-xs text-[#777]">Aggiornamento...</span>}
                </div>
                {!selectedBando ? (
                  <div className="rounded-xl border border-dashed border-[#2a2a2a] bg-[#101010] px-4 py-6">
                    <p className="text-sm text-[#7a7a7a]">Seleziona un bando dalla lista.</p>
                  </div>
                ) : (
                  <div className="flex flex-col gap-4">
                    <div>
                      <h3 className="text-lg font-semibold text-[#f5f5f5] leading-snug">{selectedBando.title}</h3>
                      <p className="mt-1 text-sm text-[#8d8d8d]">{selectedBando.issuerName}</p>
                    </div>

                    {/* Azioni valutazione */}
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => handleSetUserStatus(selectedBando.id, selectedBando.userStatus === 'Considerato' ? null : 'Considerato')}
                        className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition ${
                          selectedBando.userStatus === 'Considerato'
                            ? 'border-emerald-600/60 bg-emerald-900/30 text-emerald-200'
                            : 'border-[#2a2a2a] bg-[#101010] text-[#777] hover:border-emerald-700/50 hover:text-emerald-300'
                        }`}
                      >
                        ✓ Considera
                      </button>
                      <button
                        type="button"
                        onClick={() => handleSetUserStatus(selectedBando.id, selectedBando.userStatus === 'Escluso' ? null : 'Escluso')}
                        className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition ${
                          selectedBando.userStatus === 'Escluso'
                            ? 'border-red-700/60 bg-red-900/30 text-red-200'
                            : 'border-[#2a2a2a] bg-[#101010] text-[#777] hover:border-red-800/50 hover:text-red-300'
                        }`}
                      >
                        ✕ Escludi
                      </button>
                    </div>

                    <div className={`rounded-xl border px-4 py-4 ${getDeadlineTone(selectedBando.deadline)}`}>
                      <p className="text-[10px] uppercase tracking-[0.16em]">Scadenza</p>
                      <p className="mt-1 text-lg font-semibold">{formatDate(selectedBando.deadline)}</p>
                      <p className="mt-1 text-xs opacity-70">Pubblicato il {formatDate(selectedBando.publishedAt ?? selectedBando.createdAt)}</p>
                    </div>

                    <div className="flex flex-wrap gap-1.5 text-[11px]">
                      <span className={`rounded-md border px-2 py-0.5 ${getStatusTone(selectedBando.status)}`}>{selectedBando.status}</span>
                      {selectedBando.discipline && <span className="rounded-md border border-sky-900/40 bg-sky-900/20 px-2 py-0.5 text-sky-200">{selectedBando.discipline}</span>}
                      <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-0.5 text-[#c9c9c9]">{selectedBando.issuerType}</span>
                      <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-0.5 text-[#c9c9c9]">{selectedBando.isPublic ? 'Pubblico' : 'Privato'}</span>
                      <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-0.5 text-[#c9c9c9]">Conf. {selectedBando.confidenceScore.toFixed(2)}</span>
                      {selectedBando.role && <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-0.5 text-[#c9c9c9]">{selectedBando.role}</span>}
                    </div>

                    <div className="grid grid-cols-2 gap-2">
                      <div className="rounded-xl border border-[#222] bg-[#101010] px-3 py-2">
                        <p className="text-[10px] uppercase tracking-[0.14em] text-[#666]">Fonte</p>
                        <p className="mt-1 text-sm text-[#f5f5f5]">{selectedBando.sourceName}</p>
                      </div>
                      <div className="rounded-xl border border-[#222] bg-[#101010] px-3 py-2">
                        <p className="text-[10px] uppercase tracking-[0.14em] text-[#666]">Luogo</p>
                        <p className="mt-1 text-sm text-[#f5f5f5]">{selectedBando.location ?? 'Non indicato'}</p>
                      </div>
                    </div>

                    <div className="rounded-xl border border-[#222] bg-[#101010] px-4 py-3">
                      <p className="text-[10px] uppercase tracking-[0.14em] text-[#666]">Descrizione</p>
                      <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-[#d0d0d0]">{selectedBando.bodyText || 'Non disponibile.'}</p>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      <a href={selectedBando.sourceUrl} target="_blank" rel="noreferrer"
                        className="rounded-lg border border-[#d4af37]/40 bg-[#d4af37]/10 px-4 py-2 text-sm font-medium text-[#f3d67a] transition hover:bg-[#d4af37]/20">
                        Apri bando
                      </a>
                      {selectedBando.applicationUrl && (
                        <a href={selectedBando.applicationUrl} target="_blank" rel="noreferrer"
                          className="rounded-lg border border-[#2d6c4b]/40 bg-[#153323] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#1b432e]">
                          Vai alla candidatura
                        </a>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </section>
          </div>
        )}

        {/* ── IMPOSTAZIONI ── */}
        {bandiTab === 'impostazioni' && (
          <div className="p-6 flex flex-col gap-5">
            {/* Scrape controls */}
            <section className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              <h3 className="mb-4 text-xs font-semibold uppercase tracking-wider text-[#555]">Esegui scrape</h3>
              <div className="flex flex-wrap gap-2">
                <button type="button" onClick={() => handleScrape('P1')} disabled={scraping}
                  className="rounded-lg border border-[#d4af37]/40 bg-[#d4af37]/10 px-4 py-2 text-sm font-medium text-[#f3d67a] transition hover:bg-[#d4af37]/20 disabled:opacity-50">
                  {scraping && scrapeScope === 'P1' ? 'P1 in corso...' : 'Scrape P1 — Fonti nazionali'}
                </button>
                <button type="button" onClick={() => handleScrape('P2')} disabled={scraping}
                  className="rounded-lg border border-[#7aa7ff]/35 bg-[#0f1a33] px-4 py-2 text-sm font-medium text-[#b7cdff] transition hover:bg-[#132248] disabled:opacity-50">
                  {scraping && scrapeScope === 'P2' ? 'P2 in corso...' : 'Scrape P2 — Teatri e fondazioni'}
                </button>
                <button type="button" onClick={() => handleScrape('P3')} disabled={scraping || curatedSources.length === 0}
                  className="rounded-lg border border-emerald-700/35 bg-[#0f2a1d] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#133324] disabled:opacity-50">
                  {scraping && scrapeScope === 'P3' ? 'P3 in corso...' : `Scrape P3 — Curate (${curatedSources.length})`}
                </button>
              </div>
              {scrapeResult && (
                <div className="mt-4 flex flex-wrap gap-4 text-sm">
                  <span className="text-[#666]">Ambito: <span className="text-[#f5f5f5] font-semibold">{scrapeScope}</span></span>
                  <span className="text-[#666]">Trovati: <span className="text-[#f5f5f5] font-semibold">{scrapeResult.totalFound}</span></span>
                  <span className="text-[#666]">Artistici: <span className="text-[#f5f5f5] font-semibold">{scrapeResult.totalEligible}</span></span>
                  <span className="text-[#666]">Nuovi: <span className="text-[#f3d67a] font-semibold">{scrapeResult.totalNew}</span></span>
                </div>
              )}
            </section>

            {/* Registry fonti */}
            <section className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              {/* Add curated */}
              <div className="mb-5 rounded-xl border border-[#234331] bg-[#101810] p-4">
                <p className="text-sm font-semibold text-[#e8f6ec]">Aggiungi fonte curata P3</p>
                <p className="mt-0.5 text-xs text-[#8fb39b]">Whitelist manuale — nessun crawling libero.</p>
                <div className="mt-3 grid grid-cols-1 gap-2 md:grid-cols-[1fr_1.4fr_auto]">
                  <input value={curatedName} onChange={e => setCuratedName(e.target.value)} placeholder="Nome ente"
                    className="rounded-lg border border-[#2f4d39] bg-[#0d140d] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-emerald-500/40" />
                  <input value={curatedBaseUrl} onChange={e => setCuratedBaseUrl(e.target.value)} placeholder="https://sito.it/bandi"
                    className="rounded-lg border border-[#2f4d39] bg-[#0d140d] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-emerald-500/40" />
                  <button type="button" onClick={handleCreateCuratedSource} disabled={savingCurated}
                    className="rounded-lg border border-emerald-700/35 bg-[#163524] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#1b432e] disabled:opacity-50">
                    {savingCurated ? 'Salvataggio...' : 'Registra'}
                  </button>
                </div>
              </div>

              {/* Fonti list */}
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Registry fonti ({sources.length})</h3>
                <select value={regioneFilter} onChange={e => setRegioneFilter(e.target.value)}
                  className="rounded-lg border border-[#262626] bg-[#101010] px-2 py-1 text-xs text-[#d0d0d0] outline-none">
                  <option value="all">Tutte le regioni</option>
                  {regioneOptions.map(r => <option key={r} value={r}>{r}</option>)}
                </select>
              </div>

              {filteredSources.length === 0 ? (
                <div className="rounded-lg border border-[#222] bg-[#101010] px-4 py-3 text-sm text-[#888]">
                  {sources.length === 0 ? 'Nessuna fonte disponibile.' : 'Nessuna fonte per questa regione.'}
                </div>
              ) : (
                <div className="flex flex-col gap-2">
                  {filteredSources.map(source => (
                    <div key={source.name}
                      className={`rounded-xl border bg-[#101010] px-4 py-3 ${
                        source.lastRunError ? 'border-red-800/40' : source.lastRunAt ? 'border-emerald-800/30' : 'border-[#222]'
                      }`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-semibold text-[#f5f5f5]">{source.name}</p>
                          <div className="mt-0.5 flex flex-wrap items-center gap-1.5">
                            <span className="text-xs text-[#808080]">{source.category}</span>
                            {source.regione && (
                              <span className="rounded-full bg-[#1e1e2e] border border-[#333] px-1.5 py-0.5 text-[10px] text-[#9ca3af]">{source.regione}</span>
                            )}
                          </div>
                        </div>
                        <div className="flex items-center gap-1 flex-shrink-0">
                          <button type="button" onClick={() => handleToggleBandoSource(source.name, !source.isEnabled)}
                            title={source.isEnabled ? 'Disattiva' : 'Attiva'}
                            className={`w-7 h-7 flex items-center justify-center rounded-lg text-xs transition-colors ${
                              source.isEnabled ? 'text-emerald-400 hover:text-emerald-300' : 'text-[#555] hover:text-[#999]'
                            }`}>
                            {source.isEnabled ? '●' : '○'}
                          </button>
                          <button type="button" onClick={() => handleEditSource(source)} title="Modifica"
                            className="w-7 h-7 flex items-center justify-center rounded-lg text-[#555] hover:text-[#d4af37] transition-colors">
                            <svg viewBox="0 0 24 24" className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                            </svg>
                          </button>
                          {!source.isOfficial && (
                            <button type="button" onClick={() => handleDeleteSource(source.name)} title="Elimina"
                              className="w-7 h-7 flex items-center justify-center rounded-lg text-[#555] hover:text-red-400 transition-colors">
                              <svg viewBox="0 0 24 24" className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                                <polyline points="3 6 5 6 21 6" />
                                <path d="M19 6l-1 14H6L5 6" />
                                <path d="M10 11v6M14 11v6" />
                                <path d="M9 6V4h6v2" />
                              </svg>
                            </button>
                          )}
                        </div>
                      </div>

                      {editingSource === source.name && (
                        <div className="mt-3 flex flex-col gap-2">
                          <input value={editBaseUrl} onChange={e => setEditBaseUrl(e.target.value)} placeholder="URL base"
                            className="rounded-lg border border-[#333] bg-[#0d0d0d] px-3 py-1.5 text-xs text-[#f5f5f5] outline-none focus:border-[#d4af37]/40" />
                          <input value={editRegione} onChange={e => setEditRegione(e.target.value)} placeholder="Regione"
                            className="rounded-lg border border-[#333] bg-[#0d0d0d] px-3 py-1.5 text-xs text-[#f5f5f5] outline-none focus:border-[#d4af37]/40" />
                          <div className="flex gap-2">
                            <button type="button" onClick={() => handleSaveSource(source.name)} disabled={savingSource}
                              className="rounded-lg border border-[#d4af37]/40 bg-[#d4af37]/10 px-3 py-1 text-xs font-medium text-[#f3d67a] hover:bg-[#d4af37]/20 disabled:opacity-50">
                              {savingSource ? 'Salvataggio...' : 'Salva'}
                            </button>
                            <button type="button" onClick={() => setEditingSource(null)}
                              className="rounded-lg border border-[#333] px-3 py-1 text-xs text-[#888] hover:text-[#ccc]">
                              Annulla
                            </button>
                          </div>
                        </div>
                      )}

                      {source.lastRunAt ? (
                        <div className="mt-2 flex flex-wrap gap-x-4 gap-y-0.5 text-xs">
                          <span className="text-[#555]">Run: <span className="text-[#888]">{new Date(source.lastRunAt).toLocaleString('it-IT')}</span></span>
                          <span className="text-[#555]">Trovati: <span className="text-[#aaa]">{source.lastRunFound}</span></span>
                          <span className="text-[#555]">Artistici: <span className="text-[#aaa]">{source.lastRunEligible}</span></span>
                          <span className="text-[#555]">Nuovi: <span className={source.lastRunNew > 0 ? 'text-emerald-400' : 'text-[#aaa]'}>{source.lastRunNew}</span></span>
                          {source.lastRunError && <span className="text-red-400">⚠ {source.lastRunError}</span>}
                        </div>
                      ) : (
                        <p className="mt-1 text-[10px] text-[#444]">Mai eseguito</p>
                      )}
                      <p className="mt-1 break-all text-[10px] text-[#444]">{source.baseUrl}</p>
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </div>
    </div>
  );
}
