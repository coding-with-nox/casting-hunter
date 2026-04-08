import { useEffect, useState } from 'react';
import { castingApi } from '../api/castingApi';
import type { BandiPlan } from '../api/types';

const FALLBACK_PLAN: BandiPlan = {
  status: 'Planning locale',
  summary: 'Roadmap iniziale per integrare i bandi di concorso artistici con fonti ufficiali, parsing PDF e classificazione per ente.',
  priorityPlan: [
    { title: 'Definire il perimetro artistico', detail: 'Bloccare whitelist e blacklist dei ruoli per separare performer e profili non artistici.' },
    { title: 'Integrare le fonti nazionali ufficiali', detail: 'Partire da inPA, Gazzetta Ufficiale e MiC Spettacolo per avere copertura pubblica stabile.' },
    { title: 'Aggiungere parsing PDF e OCR fallback', detail: 'Estrarre testo dagli allegati PDF e usare OCR solo quando il testo non e leggibile.' },
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

export function Bandi() {
  const [plan, setPlan] = useState<BandiPlan | null>(null);
  const [loading, setLoading] = useState(true);
  const [warning, setWarning] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    const load = async () => {
      setLoading(true);
      setWarning(null);
      try {
        const data = await castingApi.getBandiPlan();
        if (active) setPlan(data);
      } catch (e) {
        if (active) {
          setPlan(FALLBACK_PLAN);
          setWarning(e instanceof Error ? e.message : 'Endpoint bandi non disponibile');
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    void load();
    return () => { active = false; };
  }, []);

  if (loading) return <div className="p-8 text-[#666]">Caricamento piano bandi...</div>;

  if (!plan) {
    return (
      <div className="p-6">
        <div className="bg-red-900/20 border border-red-800/50 rounded-lg p-4 text-sm text-red-300">
          Piano bandi non disponibile
        </div>
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-6 flex flex-col gap-5">
        {warning && (
          <div className="bg-yellow-900/20 border border-yellow-800/50 rounded-lg p-4 text-sm text-yellow-200">
            {warning}. Visualizzo il piano locale finche il backend non espone ancora `/api/bandi/plan`.
          </div>
        )}
        <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[11px] uppercase tracking-[0.18em] text-[#d4af37] mb-2">Nuova Area</p>
              <h2 className="text-xl font-semibold text-[#f5f5f5]">Bandi di concorso</h2>
              <p className="text-sm text-[#8a8a8a] mt-2 max-w-3xl">{plan.summary}</p>
            </div>
            <div className="rounded-lg border border-[#d4af37]/30 bg-[#d4af37]/10 px-3 py-2 text-right">
              <p className="text-[10px] uppercase tracking-[0.16em] text-[#d4af37]">Stato</p>
              <p className="text-sm font-semibold text-[#f3d67a]">{plan.status}</p>
            </div>
          </div>
        </section>

        <section className="grid grid-cols-1 xl:grid-cols-[1.3fr_0.9fr] gap-5 items-start">
          <div className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
            <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-4">Ordine di sviluppo</h3>
            <div className="flex flex-col gap-3">
              {plan.priorityPlan.map((item, index) => (
                <div key={item.title} className="rounded-xl border border-[#222] bg-[#101010] px-4 py-3">
                  <div className="flex items-start gap-3">
                    <div className="w-7 h-7 flex items-center justify-center rounded-lg bg-[#d4af37]/10 text-[#d4af37] text-sm font-semibold flex-shrink-0">
                      {index + 1}
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-[#f5f5f5]">{item.title}</p>
                      <p className="text-sm text-[#7f7f7f] mt-1">{item.detail}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
              <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-4">Come scaricarli</h3>
              <div className="flex flex-col gap-2">
                {plan.extractionFlow.map(step => (
                  <div key={step} className="rounded-lg border border-[#222] bg-[#101010] px-3 py-2 text-sm text-[#d0d0d0]">
                    {step}
                  </div>
                ))}
              </div>
            </section>

            <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
              <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-4">Tipi di ente</h3>
              <div className="flex flex-col gap-2">
                {plan.issuerTypes.map(type => (
                  <div key={type} className="rounded-lg border border-[#222] bg-[#101010] px-3 py-2 text-sm text-[#c8c8c8]">
                    {type}
                  </div>
                ))}
              </div>
            </section>
          </div>
        </section>

        <section className="bg-[#141414] border border-[#1e1e1e] rounded-xl p-5">
          <h3 className="text-xs font-semibold text-[#555] uppercase tracking-wider mb-4">Siti da scrapare</h3>
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {plan.sourceGroups.map(group => (
              <div key={group.name} className="rounded-xl border border-[#222] bg-[#101010] p-4">
                <p className="text-sm font-semibold text-[#f5f5f5] mb-3">{group.name}</p>
                <div className="flex flex-col gap-2">
                  {group.items.map(item => (
                    <div key={item} className="rounded-lg bg-[#161616] px-3 py-2 text-sm text-[#9a9a9a]">
                      {item}
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
