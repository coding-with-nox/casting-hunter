import { useState } from 'react';
import { Dashboard } from './pages/Dashboard';
import { Favorites } from './pages/Favorites';
import { Settings } from './pages/Settings';
import { NotificationBadge } from './components/NotificationBadge';
import { useCastingCalls } from './hooks/useCastingCalls';

type Tab = 'dashboard' | 'favorites' | 'settings';

function App() {
  const [tab, setTab] = useState<Tab>('dashboard');
  const { calls } = useCastingCalls();
  const newCount = calls.filter(c => {
    const scraped = new Date(c.scrapedAt);
    const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
    return scraped > oneDayAgo;
  }).length;
  const favoriteCount = calls.filter(c => c.isFavorite).length;

  return (
    <div className="min-h-screen bg-[#0f0f0f] flex flex-col">
      <header className="border-b border-[#2a2a2a] sticky top-0 z-10 bg-[#0f0f0f]/95 backdrop-blur">
        <div className="max-w-screen-xl mx-auto px-6 py-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="text-2xl">🎬</span>
            <span className="text-lg font-bold text-[#f5f5f5]">CastingRadar</span>
          </div>
          <nav className="flex gap-1">
            {([
              { id: 'dashboard' as Tab, label: 'Dashboard', badge: newCount },
              { id: 'favorites' as Tab, label: 'Preferiti', badge: favoriteCount },
              { id: 'settings' as Tab, label: 'Impostazioni', badge: 0 },
            ] as const).map(({ id, label, badge }) => (
              <button
                key={id}
                onClick={() => setTab(id)}
                className={`flex items-center px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                  tab === id
                    ? 'bg-[#d4af37]/20 text-[#d4af37]'
                    : 'text-[#9ca3af] hover:text-[#f5f5f5] hover:bg-[#1e1e1e]'
                }`}
              >
                {label}
                <NotificationBadge count={badge} />
              </button>
            ))}
          </nav>
        </div>
      </header>

      <main className="max-w-screen-xl mx-auto w-full flex-1">
        {tab === 'dashboard' && <Dashboard />}
        {tab === 'favorites' && <Favorites />}
        {tab === 'settings' && <Settings />}
      </main>
    </div>
  );
}

export default App;
