import { useState } from 'react';
import { Dashboard } from './pages/Dashboard';
import { Favorites } from './pages/Favorites';
import { Settings } from './pages/Settings';
import { NotificationBadge } from './components/NotificationBadge';
import { useCastingCalls } from './hooks/useCastingCalls';
import { castingApi } from './api/castingApi';

type Tab = 'dashboard' | 'favorites' | 'settings';

const TABS = [
  { id: 'dashboard' as Tab, label: 'Dashboard', icon: '🎬' },
  { id: 'favorites' as Tab, label: 'Preferiti', icon: '⭐' },
  { id: 'settings' as Tab, label: 'Impostazioni', icon: '⚙️' },
] as const;

function App() {
  const [tab, setTab] = useState<Tab>('dashboard');
  const [scraping, setScraping] = useState(false);
  const { calls, refetch } = useCastingCalls();

  const newCount = calls.filter(c => {
    const scraped = new Date(c.scrapedAt);
    const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
    return scraped > oneDayAgo;
  }).length;
  const favoriteCount = calls.filter(c => c.isFavorite).length;

  const badges: Record<Tab, number> = {
    dashboard: newCount,
    favorites: favoriteCount,
    settings: 0,
  };

  const handleScrape = async () => {
    setScraping(true);
    try {
      await castingApi.scrapeAll();
      await refetch();
    } finally {
      setScraping(false);
    }
  };

  return (
    <div className="h-screen bg-[#0f0f0f] flex flex-col overflow-hidden">
      {/* Header */}
      <header className="border-b border-[#1a1a1a] flex-shrink-0 bg-[#0a0a0a]">
        <div className="px-5 py-2.5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="text-xl">🎬</span>
            <span className="text-base font-bold text-[#f5f5f5] tracking-tight">CastingRadar</span>
          </div>

          <nav className="flex gap-0.5">
            {TABS.map(({ id, label, icon }) => (
              <button
                key={id}
                onClick={() => setTab(id)}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  tab === id
                    ? 'bg-[#1e1e1e] text-[#d4af37]'
                    : 'text-[#555] hover:text-[#999] hover:bg-[#151515]'
                }`}
              >
                <span className="text-base leading-none">{icon}</span>
                <span>{label}</span>
                <NotificationBadge count={badges[id]} />
              </button>
            ))}
          </nav>

          <button
            onClick={handleScrape}
            disabled={scraping}
            className="text-xs px-3 py-1.5 rounded-lg bg-[#1a1a1a] border border-[#252525] text-[#555] hover:border-[#d4af37]/40 hover:text-[#d4af37] transition-colors disabled:opacity-40"
          >
            {scraping ? '⟳ Aggiornamento...' : '⟳ Aggiorna'}
          </button>
        </div>
      </header>

      {/* Body */}
      <div className="flex-1 min-h-0">
        {tab === 'dashboard' && <Dashboard />}
        {tab === 'favorites' && <Favorites />}
        {tab === 'settings' && <Settings />}
      </div>
    </div>
  );
}

export default App;
