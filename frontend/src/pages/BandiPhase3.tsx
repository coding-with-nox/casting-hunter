import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { BandiPlan, Bando, BandoScrapeResult, BandoSource } from '../api/types';

const FALLBACK_PLAN: BandiPlan = {
  status: 'Planning locale',
  summary: 'Roadmap iniziale per integrare i bandi di concorso artistici con fonti ufficiali, parsing PDF e classificazione per ente.',
  priorityPlan: [
    { title: 'Definire il perimetro artistico', detail: 'Bloccare whitelist e blacklist dei ruoli per separare performer e profili non artistici.' },
    { title: 'Integrare le fonti nazionali ufficiali', detail: 'Partire da inPA, Gazzetta Ufficiale e MiC Spettacolo per avere copertura pubblica stabile.' },
    { title: 'Aggiungere parsing PDF e OCR fallback', detail: 'Estrarre testo dai PDF allegati e usare OCR quando manca testo leggibile.' },
    { title: 'Creare il registro fonti per categoria ente', detail: 'Classificare le fonti in PA, teatri, fondazioni, associazioni e accademie.' },
    { title: 'Costruire classificazione e deduplica', detail: 'Unire bandi duplicati tra ente, Gazzetta e allegati PDF.' },
    { title: 'Attivare scraper verticali dei teatri', detail: 'Integrare Scala, Opera di Roma, TCBO, San Carlo, Teatro Massimo e teatri stabili.' },
    { title: 'Gestire i bandi a bassa confidenza', detail: 'Introdurre revisione manuale per i risultati dubbi.' },
    { title: 'Aprire alla rete associazioni curate', detail: 'Espandere solo tramite whitelist ufficiale o derivata da elenchi MiC/FNSV.' },
  ],
  extractionFlow: [
    'Scaricare liste HTML ufficiali e pagine dettaglio.',
    'Raccogliere titolo, ente, data, scadenza, link candidatura e allegati.',
    'Estrarre testo dai PDF allegati e usare OCR quando manca testo leggibile.',
    'Applicare filtro artistico su ente, sezione del sito e contenuto del bando.',
    'Salvare punteggio di confidenza e mandare in revisione i casi ambigui.',
  ],
  issuerTypes: [
    'PA: ministeri, enti locali, accademie, universita, enti vigilati',
    'Teatri e fondazioni pubbliche o partecipate',
    'Fondazioni lirico-sinfoniche',
    'Associazioni e fondazioni private o non profit',
  ],
  sourceGroups: [
    {
      name: 'P1 - Fonti nazionali',
      items: [
        'inPA - bandi e avvisi',
        'Gazzetta Ufficiale - 4a serie concorsi',
        'MiC Spettacolo',
        'MiC - fondazioni lirico-sinfoniche ed elenchi organismi',
      ],
    },
    {
      name: 'P2 - Teatri e fondazioni ad alta resa',
      items: [
        'Teatro alla Scala',
        'Teatro dell Opera di Roma',
        'Teatro Comunale di Bologna',
        'Teatro di San Carlo',
        'Teatro Massimo',
        'Teatro Stabile di Torino',
      ],
    },
    {
      name: 'P3 - Associazioni e organismi curati',
      items: [
        'Associazioni culturali in whitelist manuale',
        'Fondazioni private con sezioni bandi o audizioni ufficiali',
        'Scuole e accademie con avvisi pubblici di selezione artistica',
      ],
    },
  ],
};

type SortMode = 'recent' | 'deadline' | 'confidence';
type ConfidenceFilter = 'all' | 'high' | 'medium' | 'low';

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

export function BandiPhase3() {
  const [plan, setPlan] = useState<BandiPlan | null>(null);
  const [bandi, setBandi] = useState<Bando[]>([]);
  const [sources, setSources] = useState<BandoSource[]>([]);
  const [selectedBandoId, setSelectedBandoId] = useState<string | null>(null);
  const [selectedBando, setSelectedBando] = useState<Bando | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [scraping, setScraping] = useState(false);
  const [scrapeResult, setScrapeResult] = useState<BandoScrapeResult | null>(null);
  const [scrapeScope, setScrapeScope] = useState<'P1' | 'P2' | 'P3' | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [curatedName, setCuratedName] = useState('');
  const [curatedBaseUrl, setCuratedBaseUrl] = useState('');
  const [savingCurated, setSavingCurated] = useState(false);
  const [search, setSearch] = useState('');
  const [sourceFilter, setSourceFilter] = useState('all');
  const [issuerTypeFilter, setIssuerTypeFilter] = useState('all');
  const [disciplineFilter, setDisciplineFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortMode, setSortMode] = useState<SortMode>('recent');
  const [confidenceFilter, setConfidenceFilter] = useState<ConfidenceFilter>('all');
  const [onlyPublic, setOnlyPublic] = useState(false);

  const load = async () => {
    setLoading(true);
    const nextWarnings: string[] = [];

    try {
      const [planResult, bandiResult, sourcesResult] = await Promise.allSettled([
        castingApi.getBandiPlan(),
        castingApi.getBandi(),
        castingApi.getBandiSources(),
      ]);

      if (planResult.status === 'fulfilled') {
        setPlan(planResult.value);
      } else {
        setPlan(FALLBACK_PLAN);
        nextWarnings.push(planResult.reason instanceof Error ? planResult.reason.message : 'Piano bandi non disponibile');
      }

      if (bandiResult.status === 'fulfilled') {
        setBandi(bandiResult.value);
      } else {
        setBandi([]);
        nextWarnings.push(bandiResult.reason instanceof Error ? bandiResult.reason.message : 'Archivio bandi non disponibile');
      }

      if (sourcesResult.status === 'fulfilled') {
        setSources(sourcesResult.value);
      } else {
        setSources([]);
        nextWarnings.push(sourcesResult.reason instanceof Error ? sourcesResult.reason.message : 'Registry fonti bandi non disponibile');
      }

      setWarnings(nextWarnings);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const sourceOptions = Array.from(new Set(bandi.map(bando => bando.sourceName))).sort((a, b) => a.localeCompare(b));
  const issuerTypeOptions = Array.from(new Set(bandi.map(bando => bando.issuerType))).sort((a, b) => a.localeCompare(b));
  const disciplineOptions = Array.from(new Set(bandi.flatMap(bando => (bando.discipline ? [bando.discipline] : [])))).sort((a, b) => a.localeCompare(b));
  const statusOptions = Array.from(new Set(bandi.map(bando => bando.status))).sort((a, b) => a.localeCompare(b));
  const reviewQueue = bandi
    .filter(bando => bando.status === 'Da rivedere')
    .sort((left, right) => left.confidenceScore - right.confidenceScore);
  const curatedSources = sources.filter(source => source.category.includes('P3'));

  const filteredBandi = [...bandi]
    .filter(bando => {
      const haystack = `${bando.title} ${bando.issuerName} ${bando.role ?? ''} ${bando.bodyText}`.toLowerCase();
      const needle = search.trim().toLowerCase();

      if (needle && !haystack.includes(needle)) return false;
      if (sourceFilter !== 'all' && bando.sourceName !== sourceFilter) return false;
      if (issuerTypeFilter !== 'all' && bando.issuerType !== issuerTypeFilter) return false;
      if (disciplineFilter !== 'all' && bando.discipline !== disciplineFilter) return false;
      if (statusFilter !== 'all' && bando.status !== statusFilter) return false;
      if (confidenceFilter === 'high' && bando.confidenceScore < 0.85) return false;
      if (confidenceFilter === 'medium' && (bando.confidenceScore < 0.70 || bando.confidenceScore >= 0.85)) return false;
      if (confidenceFilter === 'low' && bando.confidenceScore >= 0.70) return false;
      if (onlyPublic && !bando.isPublic) return false;
      return true;
    })
    .sort((left, right) => {
      if (sortMode === 'confidence') {
        return left.confidenceScore - right.confidenceScore;
      }

      if (sortMode === 'deadline') {
        const leftDeadline = left.deadline ? new Date(left.deadline).getTime() : Number.MAX_SAFE_INTEGER;
        const rightDeadline = right.deadline ? new Date(right.deadline).getTime() : Number.MAX_SAFE_INTEGER;
        if (leftDeadline !== rightDeadline) return leftDeadline - rightDeadline;
      }

      const leftDate = new Date(left.publishedAt ?? left.createdAt).getTime();
      const rightDate = new Date(right.publishedAt ?? right.createdAt).getTime();
      return rightDate - leftDate;
    });

  useEffect(() => {
    if (filteredBandi.length === 0) {
      setSelectedBandoId(null);
      setSelectedBando(null);
      return;
    }

    if (!selectedBandoId || !filteredBandi.some(bando => bando.id === selectedBandoId)) {
      setSelectedBandoId(filteredBandi[0].id);
      setSelectedBando(filteredBandi[0]);
    }
  }, [filteredBandi, selectedBandoId]);

  useEffect(() => {
    if (!selectedBandoId) return;

    const fallback = filteredBandi.find(bando => bando.id === selectedBandoId) ?? null;
    if (fallback) setSelectedBando(fallback);

    let active = true;
    const loadDetail = async () => {
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

    void loadDetail();
    return () => {
      active = false;
    };
  }, [filteredBandi, selectedBandoId]);

  const handleScrape = async () => {
    setScraping(true);
    setWarnings([]);
    setScrapeScope('P1');

    try {
      const result = await castingApi.scrapeBandiP1();
      setScrapeResult(result);
      await load();
    } catch (error) {
      setScrapeResult(null);
      setWarnings([error instanceof Error ? error.message : 'Scrape bandi P1 non disponibile']);
    } finally {
      setScraping(false);
    }
  };

  const handleScrapeP2 = async () => {
    setScraping(true);
    setWarnings([]);
    setScrapeScope('P2');

    try {
      const result = await castingApi.scrapeBandiP2();
      setScrapeResult(result);
      await load();
    } catch (error) {
      setScrapeResult(null);
      setWarnings([error instanceof Error ? error.message : 'Scrape bandi P2 non disponibile']);
    } finally {
      setScraping(false);
    }
  };

  const handleScrapeP3 = async () => {
    setScraping(true);
    setWarnings([]);
    setScrapeScope('P3');

    try {
      const result = await castingApi.scrapeBandiP3();
      setScrapeResult(result);
      await load();
    } catch (error) {
      setScrapeResult(null);
      setWarnings([error instanceof Error ? error.message : 'Scrape bandi P3 non disponibile']);
    } finally {
      setScraping(false);
    }
  };

  const handleCreateCuratedSource = async () => {
    if (!curatedName.trim() || !curatedBaseUrl.trim()) {
      setWarnings(['Nome fonte e URL sono obbligatori per registrare una whitelist curata']);
      return;
    }

    setSavingCurated(true);
    setWarnings([]);

    try {
      await castingApi.createCuratedBandoSource({
        name: curatedName.trim(),
        baseUrl: curatedBaseUrl.trim(),
        priority: 20,
        isOfficial: false,
      });
      setCuratedName('');
      setCuratedBaseUrl('');
      await load();
    } catch (error) {
      setWarnings([error instanceof Error ? error.message : 'Registrazione fonte curata non disponibile']);
    } finally {
      setSavingCurated(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-[#666]">Caricamento bandi...</div>;
  }

  if (!plan) {
    return (
      <div className="p-6">
        <div className="rounded-lg border border-red-800/50 bg-red-900/20 p-4 text-sm text-red-300">
          Piano bandi non disponibile
        </div>
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-6 flex flex-col gap-5">
        {warnings.length > 0 && (
          <div className="rounded-lg border border-yellow-800/50 bg-yellow-900/20 p-4 text-sm text-yellow-200 flex flex-col gap-1">
            {warnings.map(warning => (
              <div key={warning}>{warning}</div>
            ))}
          </div>
        )}

        <section className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="mb-2 text-[11px] uppercase tracking-[0.18em] text-[#d4af37]">Nuova Area</p>
              <h2 className="text-xl font-semibold text-[#f5f5f5]">Bandi di concorso</h2>
              <p className="mt-2 max-w-3xl text-sm text-[#8a8a8a]">{plan.summary}</p>
            </div>

            <div className="flex flex-col items-start gap-3 lg:items-end">
              <div className="rounded-lg border border-[#d4af37]/30 bg-[#d4af37]/10 px-3 py-2 text-right">
                <p className="text-[10px] uppercase tracking-[0.16em] text-[#d4af37]">Fase corrente</p>
                <p className="text-sm font-semibold text-[#f3d67a]">6 - Fonti P3 curate</p>
              </div>

              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={handleScrape}
                  disabled={scraping}
                  className="rounded-lg border border-[#d4af37]/40 bg-[#d4af37]/10 px-4 py-2 text-sm font-medium text-[#f3d67a] transition hover:bg-[#d4af37]/20 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {scraping && scrapeScope === 'P1' ? 'Aggiornamento P1...' : 'Esegui scrape P1'}
                </button>
                <button
                  type="button"
                  onClick={handleScrapeP2}
                  disabled={scraping}
                  className="rounded-lg border border-[#7aa7ff]/35 bg-[#0f1a33] px-4 py-2 text-sm font-medium text-[#b7cdff] transition hover:bg-[#132248] disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {scraping && scrapeScope === 'P2' ? 'Aggiornamento P2...' : 'Esegui scrape P2'}
                </button>
                <button
                  type="button"
                  onClick={handleScrapeP3}
                  disabled={scraping || curatedSources.length === 0}
                  className="rounded-lg border border-emerald-700/35 bg-[#0f2a1d] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#133324] disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {scraping && scrapeScope === 'P3' ? 'Aggiornamento P3...' : 'Esegui scrape P3'}
                </button>
              </div>
            </div>
          </div>
        </section>

        {scrapeResult && (
          <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
            <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Ambito</p>
              <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{scrapeScope ?? '-'}</p>
            </div>
            <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Trovati</p>
              <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{scrapeResult.totalFound}</p>
            </div>
            <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Artistici</p>
              <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{scrapeResult.totalEligible}</p>
            </div>
            <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
              <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Nuovi</p>
              <p className="mt-2 text-2xl font-semibold text-[#f3d67a]">{scrapeResult.totalNew}</p>
            </div>
          </section>
        )}

        <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Archivio bandi</p>
            <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{bandi.length}</p>
            <p className="mt-1 text-sm text-[#7f7f7f]">Record caricati da backend.</p>
          </div>
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Visibili</p>
            <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{filteredBandi.length}</p>
            <p className="mt-1 text-sm text-[#7f7f7f]">Dopo filtri e ordinamento correnti.</p>
          </div>
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Fonti registrate</p>
            <p className="mt-2 text-2xl font-semibold text-[#f5f5f5]">{sources.length}</p>
            <p className="mt-1 text-sm text-[#7f7f7f]">Registry iniziale per fonti P1 e P2.</p>
          </div>
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Whitelist P3</p>
            <p className="mt-2 text-2xl font-semibold text-[#f3d67a]">{curatedSources.length}</p>
            <p className="mt-1 text-sm text-[#7f7f7f]">Fonti curate registrate manualmente dal backend/UI.</p>
          </div>
        </section>

        <section className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-7">
            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Ricerca</span>
              <input
                value={search}
                onChange={event => setSearch(event.target.value)}
                placeholder="Titolo, ente, ruolo"
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              />
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Fonte</span>
              <select
                value={sourceFilter}
                onChange={event => setSourceFilter(event.target.value)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="all">Tutte</option>
                {sourceOptions.map(option => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Tipo ente</span>
              <select
                value={issuerTypeFilter}
                onChange={event => setIssuerTypeFilter(event.target.value)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="all">Tutti</option>
                {issuerTypeOptions.map(option => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Disciplina</span>
              <select
                value={disciplineFilter}
                onChange={event => setDisciplineFilter(event.target.value)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="all">Tutte</option>
                {disciplineOptions.map(option => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Stato</span>
              <select
                value={statusFilter}
                onChange={event => setStatusFilter(event.target.value)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="all">Tutti</option>
                {statusOptions.map(option => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Ordina per</span>
              <select
                value={sortMode}
                onChange={event => setSortMode(event.target.value as SortMode)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="recent">Piu recenti</option>
                <option value="deadline">Scadenza</option>
                <option value="confidence">Confidenza piu bassa</option>
              </select>
            </label>

            <label className="flex flex-col gap-2">
              <span className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Confidenza</span>
              <select
                value={confidenceFilter}
                onChange={event => setConfidenceFilter(event.target.value as ConfidenceFilter)}
                className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-[#d4af37]/40"
              >
                <option value="all">Tutte</option>
                <option value="high">Alta</option>
                <option value="medium">Media</option>
                <option value="low">Bassa</option>
              </select>
            </label>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-3">
            <label className="inline-flex items-center gap-2 rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#d0d0d0]">
              <input
                type="checkbox"
                checked={onlyPublic}
                onChange={event => setOnlyPublic(event.target.checked)}
                className="accent-[#d4af37]"
              />
              Solo pubblici
            </label>

            <button
              type="button"
              onClick={() => {
                setSearch('');
                setSourceFilter('all');
                setIssuerTypeFilter('all');
                setDisciplineFilter('all');
                setStatusFilter('all');
                setSortMode('recent');
                setConfidenceFilter('all');
                setOnlyPublic(false);
              }}
              className="rounded-lg border border-[#262626] bg-[#101010] px-3 py-2 text-sm text-[#d0d0d0] transition hover:border-[#d4af37]/40 hover:text-[#f5f5f5]"
            >
              Reset filtri
            </button>
          </div>
        </section>

        <section className="grid grid-cols-1 gap-5 xl:grid-cols-[1.1fr_0.9fr] items-start">
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <div className="mb-4 flex items-center justify-between gap-3">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Archivio bandi</h3>
              <p className="text-xs text-[#777]">{filteredBandi.length} risultati</p>
            </div>

            {filteredBandi.length === 0 ? (
              <div className="rounded-xl border border-dashed border-[#2a2a2a] bg-[#101010] px-4 py-6">
                <p className="text-sm font-semibold text-[#f5f5f5]">Nessun risultato</p>
                <p className="mt-2 text-sm text-[#7a7a7a]">Cambia i filtri attivi oppure esegui lo scrape P1.</p>
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
                        : 'border-[#222] bg-[#101010] hover:border-[#343434]'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-[#f5f5f5]">{bando.title}</p>
                        <p className="mt-1 text-xs text-[#8a8a8a]">{bando.issuerName} - {bando.sourceName}</p>
                        <p className="mt-3 text-sm text-[#9a9a9a]">{getPreview(bando.bodyText)}</p>
                      </div>

                      <div className={`min-w-[118px] rounded-xl border px-3 py-3 text-right ${getDeadlineTone(bando.deadline)}`}>
                        <p className="text-[10px] uppercase tracking-[0.16em]">Scadenza</p>
                        <p className="mt-2 text-sm font-semibold">{formatDate(bando.deadline)}</p>
                      </div>
                    </div>

                    <div className="mt-3 flex flex-wrap gap-2 text-[11px]">
                      <span className={`rounded-md border px-2 py-1 ${getStatusTone(bando.status)}`}>{bando.status}</span>
                      {bando.discipline && <span className="rounded-md border border-sky-900/40 bg-sky-900/20 px-2 py-1 text-sky-200">{bando.discipline}</span>}
                      <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#bdbdbd]">{bando.isPublic ? 'Pubblico' : 'Privato'}</span>
                      <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#bdbdbd]">Conf. {bando.confidenceScore.toFixed(2)}</span>
                      {bando.role && <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#bdbdbd]">{bando.role}</span>}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="sticky top-0 rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <div className="mb-4 flex items-center justify-between gap-3">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Dettaglio bando</h3>
              {detailLoading && <span className="text-xs text-[#777]">Aggiornamento...</span>}
            </div>

            {!selectedBando ? (
              <div className="rounded-xl border border-dashed border-[#2a2a2a] bg-[#101010] px-4 py-6">
                <p className="text-sm font-semibold text-[#f5f5f5]">Seleziona un bando</p>
                <p className="mt-2 text-sm text-[#7a7a7a]">Il dettaglio completo appare qui.</p>
              </div>
            ) : (
              <div className="flex flex-col gap-4">
                <div>
                  <p className="text-[11px] uppercase tracking-[0.16em] text-[#d4af37]">Dettaglio</p>
                  <h3 className="mt-2 text-xl font-semibold text-[#f5f5f5]">{selectedBando.title}</h3>
                  <p className="mt-2 text-sm text-[#8d8d8d]">{selectedBando.issuerName}</p>
                </div>

                <div className={`rounded-xl border px-4 py-4 ${getDeadlineTone(selectedBando.deadline)}`}>
                  <p className="text-[10px] uppercase tracking-[0.16em]">Scadenza</p>
                  <p className="mt-2 text-lg font-semibold">{formatDate(selectedBando.deadline)}</p>
                  <p className="mt-2 text-xs opacity-80">Pubblicato il {formatDate(selectedBando.publishedAt ?? selectedBando.createdAt)}</p>
                </div>

                <div className="flex flex-wrap gap-2 text-[11px]">
                  <span className={`rounded-md border px-2 py-1 ${getStatusTone(selectedBando.status)}`}>{selectedBando.status}</span>
                  {selectedBando.discipline && <span className="rounded-md border border-sky-900/40 bg-sky-900/20 px-2 py-1 text-sky-200">{selectedBando.discipline}</span>}
                  <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#c9c9c9]">{selectedBando.issuerType}</span>
                  <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#c9c9c9]">{selectedBando.isPublic ? 'Pubblico' : 'Privato'}</span>
                  <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#c9c9c9]">Conf. {selectedBando.confidenceScore.toFixed(2)}</span>
                  {selectedBando.role && <span className="rounded-md border border-[#2a2a2a] bg-[#171717] px-2 py-1 text-[#c9c9c9]">{selectedBando.role}</span>}
                </div>

                <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                  <div className="rounded-xl border border-[#222] bg-[#101010] px-4 py-3">
                    <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Fonte</p>
                    <p className="mt-2 text-sm text-[#f5f5f5]">{selectedBando.sourceName}</p>
                  </div>
                  <div className="rounded-xl border border-[#222] bg-[#101010] px-4 py-3">
                    <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Luogo</p>
                    <p className="mt-2 text-sm text-[#f5f5f5]">{selectedBando.location ?? 'Non indicato'}</p>
                  </div>
                </div>

                <div className="rounded-xl border border-[#222] bg-[#101010] px-4 py-4">
                  <p className="text-[10px] uppercase tracking-[0.16em] text-[#666]">Descrizione</p>
                  <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-[#d0d0d0]">{selectedBando.bodyText || 'Descrizione non disponibile.'}</p>
                </div>

                {selectedBando.reviewSignals.length > 0 && (
                  <div className="rounded-xl border border-amber-800/40 bg-amber-900/20 px-4 py-4">
                    <p className="text-[10px] uppercase tracking-[0.16em] text-amber-200">Segnali revisione</p>
                    <div className="mt-3 flex flex-wrap gap-2 text-[11px]">
                      {selectedBando.reviewSignals.map(signal => (
                        <span key={signal} className="rounded-md border border-amber-800/40 bg-[#1a1405] px-2 py-1 text-amber-100">
                          {signal}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                <div className="flex flex-wrap gap-3">
                  <a
                    href={selectedBando.sourceUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="rounded-lg border border-[#d4af37]/40 bg-[#d4af37]/10 px-4 py-2 text-sm font-medium text-[#f3d67a] transition hover:bg-[#d4af37]/20"
                  >
                    Apri bando
                  </a>

                  {selectedBando.applicationUrl && (
                    <a
                      href={selectedBando.applicationUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="rounded-lg border border-[#2d6c4b]/40 bg-[#153323] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#1b432e]"
                    >
                      Vai alla candidatura
                    </a>
                  )}
                </div>
              </div>
            )}
          </div>
        </section>

        <section className="grid grid-cols-1 gap-5 xl:grid-cols-[0.9fr_1.1fr] items-start">
          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <h3 className="mb-4 text-xs font-semibold uppercase tracking-wider text-[#555]">Registry fonti</h3>
            <div className="mb-4 rounded-xl border border-[#234331] bg-[#101810] p-4">
              <p className="text-sm font-semibold text-[#e8f6ec]">Aggiungi fonte curata P3</p>
              <p className="mt-1 text-sm text-[#8fb39b]">Solo whitelist manuale. Nessun crawling libero del web.</p>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-[1fr_1.4fr_auto]">
                <input
                  value={curatedName}
                  onChange={event => setCuratedName(event.target.value)}
                  placeholder="Nome ente o associazione"
                  className="rounded-lg border border-[#2f4d39] bg-[#0d140d] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-emerald-500/40"
                />
                <input
                  value={curatedBaseUrl}
                  onChange={event => setCuratedBaseUrl(event.target.value)}
                  placeholder="https://sito-ufficiale.it/bandi"
                  className="rounded-lg border border-[#2f4d39] bg-[#0d140d] px-3 py-2 text-sm text-[#f5f5f5] outline-none focus:border-emerald-500/40"
                />
                <button
                  type="button"
                  onClick={handleCreateCuratedSource}
                  disabled={savingCurated}
                  className="rounded-lg border border-emerald-700/35 bg-[#163524] px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-[#1b432e] disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {savingCurated ? 'Salvataggio...' : 'Registra'}
                </button>
              </div>
            </div>

            {sources.length === 0 ? (
              <div className="rounded-lg border border-[#222] bg-[#101010] px-4 py-3 text-sm text-[#888]">
                Nessuna fonte bandi disponibile dal backend.
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                {sources.map(source => (
                  <div key={source.name} className="rounded-xl border border-[#222] bg-[#101010] px-4 py-3">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-[#f5f5f5]">{source.name}</p>
                        <p className="mt-1 text-xs text-[#808080]">{source.category}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-xs text-[#d4af37]">P{source.priority}</p>
                        <p className={`mt-1 text-xs ${source.isEnabled ? 'text-emerald-400' : 'text-[#777]'}`}>
                          {source.isEnabled ? 'Attiva' : 'Disattivata'}
                        </p>
                      </div>
                    </div>
                    <p className="mt-3 break-all text-xs text-[#666]">{source.baseUrl}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="rounded-xl border border-[#1e1e1e] bg-[#141414] p-5">
            <div className="mb-4 flex items-center justify-between gap-3">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[#555]">Coda Da rivedere</h3>
              <p className="text-xs text-[#777]">{reviewQueue.length} elementi</p>
            </div>

            {reviewQueue.length === 0 ? (
              <div className="rounded-xl border border-dashed border-[#2a2a2a] bg-[#101010] px-4 py-6">
                <p className="text-sm font-semibold text-[#f5f5f5]">Coda vuota</p>
                <p className="mt-2 text-sm text-[#7a7a7a]">Nessun bando richiede revisione manuale al momento.</p>
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {reviewQueue.map(bando => (
                  <button
                    key={bando.id}
                    type="button"
                    onClick={() => setSelectedBandoId(bando.id)}
                    className="w-full rounded-xl border border-amber-800/30 bg-[#101010] px-4 py-4 text-left transition hover:border-amber-700/50"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="text-sm font-semibold text-[#f5f5f5]">{bando.title}</p>
                        <p className="mt-1 text-xs text-[#8a8a8a]">{bando.issuerName}</p>
                      </div>
                      <div className="rounded-lg border border-amber-800/40 bg-amber-900/20 px-3 py-2 text-right">
                        <p className="text-[10px] uppercase tracking-[0.16em] text-amber-200">Conf.</p>
                        <p className="mt-1 text-sm font-semibold text-amber-100">{bando.confidenceScore.toFixed(2)}</p>
                      </div>
                    </div>
                    <div className="mt-3 flex flex-wrap gap-2 text-[11px]">
                      {bando.reviewSignals.slice(0, 4).map(signal => (
                        <span key={signal} className="rounded-md border border-amber-800/40 bg-[#1a1405] px-2 py-1 text-amber-100">
                          {signal}
                        </span>
                      ))}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
